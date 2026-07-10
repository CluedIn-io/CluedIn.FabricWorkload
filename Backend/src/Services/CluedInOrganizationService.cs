using System.Collections.Generic;
using System.Net.Http;
using System;
using System.Threading.Tasks;

namespace Fabric_Extension_BE_Boilerplate.Services;

public class CluedInOrganizationService : ICluedInOrganizationService
{
    private string cluedInBaseUrl;
    private string newAccountAccessKey;

    public CluedInOrganizationService()
    {
        this.cluedInBaseUrl = Environment.GetEnvironmentVariable("CluedInBaseUrl");
        this.newAccountAccessKey = Environment.GetEnvironmentVariable("CluedInNewAccountAccessKey");
    }

    public async Task CreateOrganization(CreateOrganizationRequest createOrganizationRequest)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{cluedInBaseUrl}/api/account/new");
        request.Headers.Add("x-cluedin-newaccountaccesskey", newAccountAccessKey);
        var collection = new List<KeyValuePair<string, string>>
        {
            new("grant_type", "password"),
            new("allowEmailDomainSignup", "False"),
            new("email", createOrganizationRequest.UserName),
            new("username", createOrganizationRequest.UserName),
            new("password", createOrganizationRequest.Password),
            new("confirmpassword", createOrganizationRequest.Password),
            new("emailDomain", createOrganizationRequest.UserName),
            new("applicationSubDomain", createOrganizationRequest.OrganizationName),
            new("organizationName", createOrganizationRequest.OrganizationName)
        };
        var content = new FormUrlEncodedContent(collection);
        request.Content = content;
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        Console.WriteLine(await response.Content.ReadAsStringAsync());

    }
}

public record class CreateOrganizationRequest (
    string OrganizationName,
    string UserName,
    string Password);