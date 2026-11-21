using System.Net.Http;
using System.Text.Json;

namespace RegRuDns;

internal sealed record DomainItem(string Name, string State, string Expires)
{
    public override string ToString() => Name;
}

internal sealed record DnsRecord(string Subname, string Type, string Content, int Priority, string State)
{
    public string DisplayName(string domain)
    {
        if (string.IsNullOrWhiteSpace(Subname) || Subname == "@")
        {
            return domain;
        }

        if (Subname == "*")
        {
            return "*." + domain;
        }

        if (Subname.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase)
            || Subname.Equals(domain, StringComparison.OrdinalIgnoreCase))
        {
            return Subname;
        }

        return Subname + "." + domain;
    }
}

internal sealed record ZoneSnapshot(
    string Domain,
    string Ttl,
    string MinimumTtl,
    IReadOnlyList<DnsRecord> Records);

internal sealed record RecordDraft(
    string Type,
    string Name,
    string Value,
    int Priority,
    int Weight,
    int Port);

internal sealed class RegRuException : Exception
{
    public RegRuException(string message) : base(message)
    {
    }
}

internal sealed class RegRuClient
{
    public const string Endpoint = "https://api.reg.ru/api/regru2";

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(40),
    };

    private static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly string _username;
    private readonly string _password;

    public RegRuClient(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task<IReadOnlyList<DomainItem>> GetDomainsAsync()
    {
        using var doc = await PostAsync("service/get_list", new Dictionary<string, object?>
        {
            ["servtype"] = "domain",
        });
        EnsureTop(doc.RootElement);
        if (!doc.RootElement.TryGetProperty("answer", out var answer)
            || !answer.TryGetProperty("services", out var services)
            || services.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var list = new List<DomainItem>();
        foreach (var service in services.EnumerateArray())
        {
            var name = Text(service, "dname");
            if (name.Length == 0)
            {
                continue;
            }

            var servtype = Text(service, "servtype");
            if (servtype.Length > 0 && !servtype.Equals("domain", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            list.Add(new DomainItem(name, Text(service, "state"), Text(service, "expiration_date")));
        }

        return list
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ZoneSnapshot> GetZoneAsync(string domain)
    {
        using var doc = await PostAsync("zone/get_resource_records", new Dictionary<string, object?>
        {
            ["domains"] = Domains(domain),
        });
        var zone = DomainAnswer(doc.RootElement, domain);
        var ttl = "";
        var minimum = "";
        if (zone.TryGetProperty("soa", out var soa) && soa.ValueKind == JsonValueKind.Object)
        {
            ttl = Text(soa, "ttl");
            minimum = Text(soa, "minimum_ttl");
        }

        var records = new List<DnsRecord>();
        if (zone.TryGetProperty("rrs", out var rrs) && rrs.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in rrs.EnumerateArray())
            {
                records.Add(new DnsRecord(
                    Text(row, "subname"),
                    Text(row, "rectype"),
                    Text(row, "content"),
                    Int(row, "prio"),
                    Text(row, "state")));
            }
        }

        records.Sort(static (a, b) =>
        {
            var byName = string.Compare(a.Subname, b.Subname, StringComparison.OrdinalIgnoreCase);
            return byName != 0
                ? byName
                : string.Compare(a.Type, b.Type, StringComparison.OrdinalIgnoreCase);
        });
        return new ZoneSnapshot(domain, ttl, minimum, records);
    }

    public Task AddAsync(string domain, RecordDraft draft)
    {
        var input = new Dictionary<string, object?>
        {
            ["domains"] = Domains(domain),
        };
        AddFields(input, draft);
        return PostDomainAsync(AddMethod(draft.Type), input, domain);
    }

    public Task RemoveAsync(string domain, DnsRecord record)
    {
        var input = new Dictionary<string, object?>
        {
            ["domains"] = Domains(domain),
        };
        RemoveFields(input, record);
        return PostDomainAsync("zone/remove_record", input, domain);
    }

    public async Task ReplaceAsync(string domain, DnsRecord current, RecordDraft draft)
    {
        var remove = new Dictionary<string, object?>
        {
            ["action"] = "remove_record",
        };
        RemoveFields(remove, current);
        var add = new Dictionary<string, object?>
        {
            ["action"] = AddMethod(draft.Type).Split('/')[1],
        };
        AddFields(add, draft);

        using var doc = await PostAsync("zone/update_records", new Dictionary<string, object?>
        {
            ["domain_name"] = domain,
            ["action_list"] = new object[] { remove, add },
        });
        var zone = DomainAnswer(doc.RootElement, domain);
        if (zone.TryGetProperty("action_list", out var actions) && actions.ValueKind == JsonValueKind.Array)
        {
            foreach (var action in actions.EnumerateArray())
            {
                var result = Text(action, "result");
                if (result.Length > 0 && !result.Equals("success", StringComparison.OrdinalIgnoreCase))
                {
                    throw new RegRuException(Explain(Text(action, "error_code"), Text(action, "error_text")));
                }
            }
        }
    }

    public Task UpdateSoaAsync(string domain, string ttl, string minimumTtl)
    {
        return PostDomainAsync("zone/update_soa", new Dictionary<string, object?>
        {
            ["domains"] = Domains(domain),
            ["ttl"] = ttl,
            ["minimum_ttl"] = minimumTtl,
        }, domain);
    }

    private async Task PostDomainAsync(string method, Dictionary<string, object?> input, string domain)
    {
        using var doc = await PostAsync(method, input);
        DomainAnswer(doc.RootElement, domain);
    }

    private async Task<JsonDocument> PostAsync(string method, Dictionary<string, object?> input)
    {
        input["username"] = _username;
        input["password"] = _password;
        input["output_content_type"] = "json";

        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["input_format"] = "json",
            ["input_data"] = JsonSerializer.Serialize(input, Json),
        });

        string text;
        try
        {
            using var response = await Http.PostAsync(Endpoint + "/" + method, body);
            text = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode && string.IsNullOrWhiteSpace(text))
            {
                throw new RegRuException(Lang.NetworkError(((int)response.StatusCode).ToString()));
            }
        }
        catch (RegRuException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new RegRuException(Lang.NetworkError(ex.Message));
        }

        try
        {
            return JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            throw new RegRuException(Lang.BadResponse);
        }
    }

    private static object[] Domains(string domain) =>
    [
        new Dictionary<string, object?> { ["dname"] = domain },
    ];

    private static void AddFields(Dictionary<string, object?> target, RecordDraft draft)
    {
        var name = string.IsNullOrWhiteSpace(draft.Name) ? "@" : draft.Name.Trim();
        var value = draft.Value.Trim();
        switch (draft.Type)
        {
            case "A":
            case "AAAA":
                target["subdomain"] = name;
                target["ipaddr"] = value;
                break;
            case "CNAME":
                target["subdomain"] = name;
                target["canonical_name"] = value;
                break;
            case "MX":
                target["subdomain"] = name;
                target["mail_server"] = value;
                target["priority"] = draft.Priority;
                break;
            case "TXT":
                target["subdomain"] = name;
                target["text"] = draft.Value;
                break;
            case "SPF":
                target["subdomain"] = name;
                target["text"] = value;
                break;
            case "NS":
                target["subdomain"] = name;
                target["dns_server"] = value;
                target["record_number"] = draft.Priority;
                break;
            case "SRV":
                target["service"] = name;
                target["priority"] = draft.Priority;
                target["weight"] = draft.Weight;
                target["port"] = draft.Port;
                target["target"] = value;
                break;
            default:
                throw new RegRuException(Lang.ApiFailed);
        }
    }

    private static void RemoveFields(Dictionary<string, object?> target, DnsRecord record)
    {
        target["subdomain"] = string.IsNullOrWhiteSpace(record.Subname) ? "@" : record.Subname;
        target["record_type"] = record.Type;
        target["content"] = record.Content;
        if (record.Type is "MX" or "SRV" || record.Priority != 0)
        {
            target["priority"] = record.Priority;
        }
    }

    private static string AddMethod(string type) => type switch
    {
        "A" => "zone/add_alias",
        "AAAA" => "zone/add_aaaa",
        "CNAME" => "zone/add_cname",
        "MX" => "zone/add_mx",
        "NS" => "zone/add_ns",
        "TXT" => "zone/add_txt",
        "SPF" => "zone/add_spf",
        "SRV" => "zone/add_srv",
        _ => throw new RegRuException(Lang.ApiFailed),
    };

    private static JsonElement DomainAnswer(JsonElement root, string domain)
    {
        EnsureTop(root);
        if (!root.TryGetProperty("answer", out var answer)
            || !answer.TryGetProperty("domains", out var domains)
            || domains.ValueKind != JsonValueKind.Array)
        {
            throw new RegRuException(Lang.ApiFailed);
        }

        JsonElement? found = null;
        foreach (var item in domains.EnumerateArray())
        {
            found ??= item;
            if (Text(item, "dname").Equals(domain, StringComparison.OrdinalIgnoreCase))
            {
                found = item;
                break;
            }
        }

        if (found is null)
        {
            throw new RegRuException(Lang.ApiFailed);
        }

        var zone = found.Value;
        var result = Text(zone, "result");
        if (result.Length > 0 && !result.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            throw new RegRuException(Explain(Text(zone, "error_code"), Text(zone, "error_text")));
        }

        return zone;
    }

    private static void EnsureTop(JsonElement root)
    {
        var result = Text(root, "result");
        if (result.Equals("success", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw new RegRuException(Explain(Text(root, "error_code"), Text(root, "error_text")));
    }

    private static string Explain(string code, string text)
    {
        var friendly = code switch
        {
            "PASSWORD_AUTH_FAILED" => Lang.ErrAuth,
            "ACCESS_DENIED_FROM_IP" => Lang.ErrIp,
            "DOMAIN_IS_NOT_USE_REGRU_NSS" => Lang.ErrNs,
            "DOMAIN_NOT_FOUND" => Lang.ErrDomain,
            "INVALID_DOMAIN_NAME_FORMAT" => Lang.ErrDomainName,
            _ => "",
        };

        if (friendly.Length == 0)
        {
            return string.IsNullOrWhiteSpace(text) ? (code.Length == 0 ? Lang.ApiFailed : code) : text;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return friendly;
        }

        return friendly + Environment.NewLine + text;
    }

    private static string Text(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return "";
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? "",
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => "",
        };
    }

    private static int Int(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            return 0;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)
            ? parsed
            : 0;
    }
}
