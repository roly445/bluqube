// <copyright file="OpenApiSecurityScheme.cs" company="Tom Roly">
// Copyright (c) Tom Roly. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// </copyright>

namespace BluQube.Constants;

/// <summary>
/// Defines the security schemes available for OpenAPI specification generation.
/// </summary>
public enum OpenApiSecurityScheme
{
    /// <summary>
    /// HTTP Bearer authentication with JWT tokens.
    /// Uses Authorization header with Bearer scheme.
    /// </summary>
    Bearer = 0,

    /// <summary>
    /// Cookie-based authentication.
    /// Uses cookies to maintain authenticated sessions.
    /// </summary>
    Cookie = 1,

    /// <summary>
    /// API Key authentication via header.
    /// Uses a custom header for API key authentication.
    /// </summary>
    ApiKey = 2,
}

