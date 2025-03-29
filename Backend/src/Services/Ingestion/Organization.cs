// <copyright company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System;

namespace Fabric_Extension_BE_Boilerplate.Services.Ingestion;

public class Organization
{
    public string ClientId { get; set; }

    public string UserName { get; set; }

    public string Password { get; set; }

    public string EmailDomain { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid UserId { get; set; }

    public string AccessToken { get; set; }

}
