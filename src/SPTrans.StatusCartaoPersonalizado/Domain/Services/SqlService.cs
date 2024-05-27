using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SPTrans.StatusCartaoPersonalizado.Configurations.Factories;

namespace PublicadorConciliacaoFaturamento.Domain.Services
{
    public interface ISqlService
    {
        Task ExecuteAsync(string sql, object parameters);
        Task<object> CreateAsync(string sql, string identityColumn, DynamicParameters param);
        Task<IEnumerable<T>> ListAsync<T>(string sql, object parameters);
        Task<IEnumerable<T>> ListAsync<T, T2>(string sql, object parameters, Func<T, T2, T> mapFunc, string splitOn = "Id");
        Task<T> GetAsync<T>(string sql, object parameters);
        Task<int> CountAsync(string sql, object parameters);
        Task<bool> ExistsAsync(string sql, object parameters);
        Task<string> ObtainAsync(string sql, object parameters);
    }

    public class SqlService : ISqlService
    {
        private readonly IDatabaseFactory _databaseFactory;
        private readonly ILogger<SqlService> _logger;

        public SqlService(IDatabaseFactory databaseFactory, ILogger<SqlService> logger)
        {
            _databaseFactory = databaseFactory;
            _logger = logger;
        }

        public async Task ExecuteAsync(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY EXECUTE COMMAND | { sql }");
            _logger.LogDebug($"QUERY EXECUTE PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();
            var command = new CommandDefinition(sql, parameters, transaction);
            await _databaseFactory.Connection().ExecuteAsync(command);

            _logger.LogDebug($"QUERY EXECUTE EXECUTED");
        }

        public async Task<object> CreateAsync(string sql, string identityColumn, DynamicParameters param)
        {
            _logger.LogDebug($"QUERY CREATE COMMAND | { sql }");
            _logger.LogDebug($"QUERY CREATE PARAMETERS | { param }");

            var transaction = _databaseFactory.BeginTransaction();
            await _databaseFactory.Connection().ExecuteAsync(sql, param, transaction);

            _logger.LogDebug($"QUERY CREATE EXECUTED");

            return param.Get<object>(identityColumn);
        }

        public async Task<IEnumerable<T>> ListAsync<T>(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY LIST COMMAND | { sql }");
            _logger.LogDebug($"QUERY LIST PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            var result = await _databaseFactory.Connection().QueryAsync<T>(command);

            _logger.LogDebug($"QUERY LIST EXECUTED");

            return result;
        }

        public async Task<IEnumerable<T>> ListAsync<T, T2>(string sql, object parameters, Func<T, T2, T> mapFunc, string splitOn = "Id")
        {
            _logger.LogDebug($"QUERY LIST COMMAND | { sql }");
            _logger.LogDebug($"QUERY LIST PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            IEnumerable<T> result = await _databaseFactory.Connection().QueryAsync(command, mapFunc, splitOn);

            _logger.LogDebug($"QUERY LIST EXECUTED");

            return result;
        }

        public async Task<T> GetAsync<T>(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY GET COMMAND | { sql }");
            _logger.LogDebug($"QUERY GET PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            var result = await _databaseFactory.Connection().QueryFirstOrDefaultAsync<T>(command);

            _logger.LogDebug($"QUERY GET EXECUTED");

            return result;
        }

        public async Task<int> CountAsync(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY COUNT COMMAND | { sql }");
            _logger.LogDebug($"QUERY COUNT PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            var result = await _databaseFactory.Connection().QueryFirstOrDefaultAsync<int>(command);

            _logger.LogDebug($"QUERY COUNT EXECUTED");

            return result;
        }

        public async Task<bool> ExistsAsync(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY EXISTS COMMAND | { sql }");
            _logger.LogDebug($"QUERY EXISTS PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            var result = await _databaseFactory.Connection().QueryFirstOrDefaultAsync<bool>(command);

            _logger.LogDebug($"QUERY EXISTS EXECUTED");

            return result;
        }

        public async Task<string> ObtainAsync(string sql, object parameters)
        {
            _logger.LogDebug($"QUERY OBTAIN COMMAND | { sql }");
            _logger.LogDebug($"QUERY OBTAIN PARAMETERS | { JsonConvert.SerializeObject(parameters) }");

            var transaction = _databaseFactory.BeginTransaction();

            var command = new CommandDefinition(sql, parameters, transaction);

            var result = await _databaseFactory.Connection().QueryFirstOrDefaultAsync<string>(command);

            _logger.LogDebug($"QUERY OBTAIN EXECUTED");

            return result;
        }
    }
}