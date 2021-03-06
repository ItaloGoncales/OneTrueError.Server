﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace OneTrueError.App.Core.ApiKeys
{
    /// <summary>
    ///     A generated API key which can be used to call OneTrueError´s HTTP api.
    /// </summary>
    public class ApiKey
    {
        private List<int> _applications = new List<int>();

        /// <summary>
        ///     Application ids that we've been granted to work with
        /// </summary>
        public IEnumerable<int> AllowedApplications
        {
            get { return _applications; }
            private set { _applications = new List<int>(value); }
        }

        /// <summary>
        ///     Application that will be using this key
        /// </summary>
        public string ApplicationName { get; set; }


        /// <summary>
        ///     When this key was generated
        /// </summary>
        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        ///     AccountId that generated this key
        /// </summary>
        public int CreatedById { get; set; }

        /// <summary>
        ///     Api key
        /// </summary>
        public string GeneratedKey { get; set; }


        /// <summary>
        ///     PK
        /// </summary>
        public int Id { get; set; }


        /// <summary>
        ///     Used when generating signatures.
        /// </summary>
        public string SharedSecret { get; set; }

        /// <summary>
        ///     Add an application that this ApiKey can be used for.
        /// </summary>
        /// <param name="applicationId">application id</param>
        public void Add(int applicationId)
        {
            if (applicationId <= 0) throw new ArgumentOutOfRangeException(nameof(applicationId));

            _applications.Add(applicationId);
        }

        /// <summary>
        ///     Validate a given signature using the HTTP body.
        /// </summary>
        /// <param name="specifiedSignature">Signature passed from the client</param>
        /// <param name="body">HTTP body (i.e. the data that the signature was generated on)</param>
        /// <returns><c>true</c> if the signature was generated using the shared secret; otherwise <c>false</c>.</returns>
        public bool ValidateSignature(string specifiedSignature, byte[] body)
        {
            var hashAlgo = new HMACSHA256(Encoding.UTF8.GetBytes(SharedSecret.ToLower()));
            var hash = hashAlgo.ComputeHash(body);
            var signature = Convert.ToBase64String(hash);

            var hashAlgo1 = new HMACSHA256(Encoding.UTF8.GetBytes(SharedSecret.ToUpper()));
            var hash1 = hashAlgo1.ComputeHash(body);
            var signature1 = Convert.ToBase64String(hash1);

            return specifiedSignature.Equals(signature) || specifiedSignature.Equals(signature1);
        }
    }
}