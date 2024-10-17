using Newtonsoft.Json;
using PhoneApp.Domain.Attributes;
using PhoneApp.Domain.DTO;
using PhoneApp.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace NewUserPlugin
{
    [Author(Name = "Matvei Kalashnikov")]
    public class Plugin : IPluggable
    {
        private const string NEW_USERS_URL = "https://dummyjson.com/users";
        private const string LOCAL_DATA_FILE = "data.json"; 
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public IEnumerable<DataTransferObject> Run(IEnumerable<DataTransferObject> args)
        {
            logger.Info("UserLoader Plugin started.");

            var users = LoadNewUsers();

            if (users != null && users.Any())
            {
                logger.Info($"Loaded {users.Count()} users.");
            }
            else
            {
                logger.Info("No users were loaded.");
            }

            foreach (var user in users)
            {
                logger.Info("User name is " + user.Name);
            }
            return users;
        }

        private IEnumerable<EmployeesDTO> LoadNewUsers()
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var response = client.GetAsync(NEW_USERS_URL).GetAwaiter().GetResult();
                    response.EnsureSuccessStatusCode();

                    var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var jsonData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

                    if (jsonData.TryGetValue("users", out var usersList))
                    {
                        var usersData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(usersList.ToString());

                        return usersData.Select(user => new EmployeesDTO
                        {
                            Name = $"{user["firstName"]} {user["lastName"]}"
                        }).ToList();
                    }
                }
                catch (HttpRequestException httpEx)
                {
                    logger.Error($"Network error while loading new users: {httpEx.Message}");
                }
                catch (Exception ex)
                {
                    logger.Error($"Error while loading new users: {ex.Message}");
                }
            }

            return LoadUsersFromFile();
        }

        private IEnumerable<EmployeesDTO> LoadUsersFromFile()
        {
            logger.Info("Loading users from backup file");
            try
            {
                var jsonData = File.ReadAllText(LOCAL_DATA_FILE);
                var jsonObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonData);

                if (jsonObject.TryGetValue("users", out var usersList))
                {
                    var usersData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(usersList.ToString());

                    return usersData.Select(user => new EmployeesDTO
                    {
                        Name = $"{user["firstName"]} {user["lastName"]}"
                    }).ToList();
                }

                return Array.Empty<EmployeesDTO>();
            }
            catch (Exception ex)
            {
                logger.Error($"Error while loading users from file: {ex.Message}");
                return Array.Empty<EmployeesDTO>();
            }
        }
    }
}
