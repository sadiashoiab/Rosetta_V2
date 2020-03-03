using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using ClearCareOnline.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Rosetta.Tests.ClearCareOnline.Api
{
    [ExcludeFromCodeCoverage]
    // note: this relies on azure storage being there, we do not want unit tests to rely on actual infrastructure so commenting out for now while we test
    //[TestClass]
    public class AzureStorageBlobCacheServiceTests
    {
        private static TestContext _testContext;

        // note: this is not a great way to do this, yet it does work and seeing as a test project doesn't load launchSettings.json automatically, this will work.
        private static List<JProperty> GetLaunchSettingEnvironmentVariables()
        {
            using (var file = File.OpenText("Properties\\launchSettings.json"))
            {
                var reader = new JsonTextReader(file);
                var jObject = JObject.Load(reader);

                var variables = jObject
                    .GetValue("profiles")
                    .SelectMany(profiles => profiles.Children())
                    .SelectMany(profile => profile.Children<JProperty>())
                    .Where(prop => prop.Name == "environmentVariables")
                    .SelectMany(prop => prop.Value.Children<JProperty>())
                    .ToList();

                return variables;
            }
        }

        [ClassInitialize]
        public static void LoadEnvironmentVariables(TestContext context)
        {
            _testContext = context;

            var variables = GetLaunchSettingEnvironmentVariables();
            foreach (var variable in variables)
            {
                Environment.SetEnvironmentVariable(variable.Name, variable.Value.ToString());
            }
        }

        [ClassCleanup]
        public static void UnLoadEnvironmentVariables()
        {
            var variables = GetLaunchSettingEnvironmentVariables();
            foreach (var variable in variables)
            {
                Environment.SetEnvironmentVariable(variable.Name, null);
            }
        }


        [TestMethod]
        public async Task RetrieveSendTest()
        {
            var loggerMock = new Mock<ILogger<AzureStorageBlobCacheService>>();
            var cacheService = new AzureStorageBlobCacheService(loggerMock.Object);
            var json = await cacheService.RetrieveJsonFromCache();
            Assert.IsNotNull(json);

            var testString = "this is a test";
            await cacheService.SendJsonToCache(testString);

            var contents = await cacheService.RetrieveJsonFromCache();
            Assert.AreEqual(testString, contents);

            await cacheService.SendJsonToCache(json);
            contents = await cacheService.RetrieveJsonFromCache();

            Assert.AreEqual(json, contents);
        }
    }
}
