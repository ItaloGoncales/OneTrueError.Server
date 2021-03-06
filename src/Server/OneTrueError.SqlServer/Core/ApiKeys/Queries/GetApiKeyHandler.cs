﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetCqs;
using Griffin.Container;
using Griffin.Data;
using Griffin.Data.Mapper;
using OneTrueError.Api.Core.ApiKeys.Queries;
using OneTrueError.App.Core.ApiKeys;
using OneTrueError.SqlServer.Core.ApiKeys.Mappings;

namespace OneTrueError.SqlServer.Core.ApiKeys.Queries
{
    /// <summary>
    /// Handler for <see cref="GetApiKey"/>.
    /// </summary>
    [Component(RegisterAsSelf = true)]
    public class GetApiKeyHandler : IQueryHandler<GetApiKey, GetApiKeyResult>
    {
        private IAdoNetUnitOfWork _uow;
        private static MirrorMapper<GetApiKeyResultApplication> _appMapping = new MirrorMapper<GetApiKeyResultApplication>();

        /// <summary>
        /// Creates a new instance of <see cref="GetApiKeyHandler"/>.
        /// </summary>
        /// <param name="uow">valid uow</param>
        public GetApiKeyHandler(IAdoNetUnitOfWork uow)
        {
            if (uow == null) throw new ArgumentNullException(nameof(uow));

            _uow = uow;
        }

        /// <summary>Method used to execute the query</summary>
        /// <param name="query">Query to execute.</param>
        /// <returns>Task which will contain the result once completed.</returns>
        public async Task<GetApiKeyResult> ExecuteAsync(GetApiKey query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            ApiKey key;
            if (!string.IsNullOrEmpty(query.ApiKey))
                key = await _uow.FirstAsync<ApiKey>("GeneratedKey=@1", query.ApiKey);
            else
                key = await _uow.FirstAsync<ApiKey>("Id=@1", query.Id);

            var result = new GetApiKeyResult
            {
                ApplicationName = key.ApplicationName,
                CreatedAtUtc = key.CreatedAtUtc,
                CreatedById = key.CreatedById,
                GeneratedKey = key.GeneratedKey,
                Id = key.Id,
                SharedSecret = key.SharedSecret
            };

            var sql = @"SELECT Id as ApplicationId, Name as ApplicationName 
FROM Applications 
JOIN ApiKeyApplications ON (Id = ApplicationId)
WHERE ApiKeyId = @1";
            var apps = await _uow.ToListAsync(_appMapping, sql, key.Id);
            result.AllowedApplications = apps.ToArray();
            return result;
        }
    }
}
