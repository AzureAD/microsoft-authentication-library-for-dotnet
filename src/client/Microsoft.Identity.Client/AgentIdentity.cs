// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// Represents the identity of an agent application and the user it acts on behalf of.
    /// Used with <see cref="IConfidentialClientApplication.AcquireTokenForAgent(System.Collections.Generic.IEnumerable{string}, AgentIdentity)"/>
    /// to acquire tokens for agent scenarios using Federated Managed Identity (FMI) and User Federated Identity Credentials (UserFIC).
    /// </summary>
    public sealed class AgentIdentity
    {
        private AgentIdentity(string agentApplicationId)
        {
            if (string.IsNullOrEmpty(agentApplicationId))
            {
                throw new ArgumentNullException(nameof(agentApplicationId));
            }

            AgentApplicationId = agentApplicationId;
        }

        /// <summary>
        /// Creates an <see cref="AgentIdentity"/> that identifies the user by their object ID (OID).
        /// This is the recommended approach for identifying users in agent scenarios.
        /// </summary>
        /// <param name="agentApplicationId">The client ID of the agent application.</param>
        /// <param name="userObjectId">The object ID (OID) of the user the agent acts on behalf of.</param>
        /// <returns>An <see cref="AgentIdentity"/> configured with the user's OID.</returns>
        public AgentIdentity(string agentApplicationId, Guid userObjectId)
            : this(agentApplicationId)
        {
            if (userObjectId == Guid.Empty)
            {
                throw new ArgumentException("userObjectId must not be empty.", nameof(userObjectId));
            }

            UserObjectId = userObjectId;
        }

        /// <summary>
        /// Creates an <see cref="AgentIdentity"/> that identifies the user by their UPN (User Principal Name).
        /// </summary>
        /// <param name="agentApplicationId">The client ID of the agent application.</param>
        /// <param name="username">The UPN of the user the agent acts on behalf of.</param>
        /// <returns>An <see cref="AgentIdentity"/> configured with the user's UPN.</returns>
        public static AgentIdentity WithUsername(string agentApplicationId, string username)
        {
            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            return new AgentIdentity(agentApplicationId)
            {
                Username = username
            };
        }

        /// <summary>
        /// Creates an <see cref="AgentIdentity"/> for app-only (no user) scenarios, where only Legs 1-2 of the
        /// agent token acquisition are performed.
        /// </summary>
        /// <param name="agentApplicationId">The client ID of the agent application.</param>
        /// <returns>An <see cref="AgentIdentity"/> configured for app-only access.</returns>
        public static AgentIdentity AppOnly(string agentApplicationId)
        {
            return new AgentIdentity(agentApplicationId);
        }

        /// <summary>
        /// Gets the client ID of the agent application.
        /// </summary>
        public string AgentApplicationId { get; }

        /// <summary>
        /// Gets the object ID (OID) of the user, if specified.
        /// </summary>
        public Guid? UserObjectId { get; private set; }

        /// <summary>
        /// Gets the UPN of the user, if specified.
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this identity includes a user identifier (OID or UPN).
        /// </summary>
        internal bool HasUserIdentifier => UserObjectId.HasValue || !string.IsNullOrEmpty(Username);
    }
}
