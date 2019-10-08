using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Rosetta.Controllers;
using Rosetta.Models;
using Rosetta.Services;

namespace Rosetta.Tests.Rosetta
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class HomeControllerTests
    {
        [TestMethod] 
        public async Task Status_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);
            var status = new Status(new List<string> {"127.0.0.1"});

            rosettaStoneServiceMock.Setup(mock => mock.GetStatus())
                .ReturnsAsync(status)
                .Verifiable();

            // ACT
            var actionResult = await unitUnderTest.Status();

            // ASSERT
            rosettaStoneServiceMock.Verify(mock => mock.GetStatus(), Times.Once);
            Assert.IsNotNull(actionResult);
            var result = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(result);
            var resultStatus = result.Value as Status;
            Assert.IsNotNull(resultStatus);
            Assert.AreEqual("active", resultStatus.status);
            Assert.AreEqual(status.client_ip_addresses.First(), resultStatus.client_ip_addresses.First());
        }

        [TestMethod] 
        public void ClearCache_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);

            // ACT
            unitUnderTest.ClearCache();

            // ASSERT
            rosettaStoneServiceMock.Verify(mock => mock.ClearCache(), Times.Once);
        }

        [TestMethod]
        public async Task GetFranchise_Found_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);
            var franchiseNumber = 1;
            var expected = new RosettaFranchise
            {
                clear_care_agency = 222,
                franchise_number = franchiseNumber.ToString()
            };

            rosettaStoneServiceMock.Setup(mock => mock.GetFranchise(franchiseNumber))
                .ReturnsAsync(expected)
                .Verifiable();

            // ACT
            var actionResult = await unitUnderTest.GetFranchise(franchiseNumber);

            // ASSERT
            rosettaStoneServiceMock.Verify();
            var result = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(result);
            var resultFranchise = result.Value as RosettaFranchise;
            Assert.IsNotNull(resultFranchise);
            Assert.AreEqual(expected.clear_care_agency, resultFranchise.clear_care_agency);
        }

        [TestMethod]
        public async Task GetFranchise_NotFound_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);
            var franchiseNumber = 1;
            rosettaStoneServiceMock.Setup(mock => mock.GetFranchise(franchiseNumber))
                .ReturnsAsync((RosettaFranchise) null)
                .Verifiable();

            // ACT
            var actionResult = await unitUnderTest.GetFranchise(franchiseNumber);

            // ASSERT
            rosettaStoneServiceMock.Verify();
            var result = actionResult.Result as NotFoundResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
        }

        [TestMethod]
        public async Task GetFranchises_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);
            var franchiseNumber = 1;
            var expected = new RosettaFranchise
            {
                clear_care_agency = 222,
                franchise_number = franchiseNumber.ToString()
            };

            rosettaStoneServiceMock.Setup(mock => mock.GetFranchises())
                .ReturnsAsync(new List<RosettaFranchise>{expected})
                .Verifiable();

            // ACT
            var actionResult = await unitUnderTest.GetFranchises();

            // ASSERT
            rosettaStoneServiceMock.Verify();
            var result = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(result);
            var rosettaFranchises = result.Value as IList<RosettaFranchise>;
            Assert.IsNotNull(rosettaFranchises);
            Assert.AreEqual(expected.clear_care_agency, rosettaFranchises.First().clear_care_agency);
        }

        [TestMethod]
        public async Task GetAgencies_Success()
        {
            // ARRANGE
            var rosettaStoneServiceMock = new Mock<IRosettaStoneService>();
            var unitUnderTest = new HomeController(rosettaStoneServiceMock.Object);
            var franchiseNumber = 1;
            var expected = new RosettaAgency
            {
                clear_care_agency = 222,
                franchise_numbers = new [] {franchiseNumber.ToString(), "2"}
            };

            rosettaStoneServiceMock.Setup(mock => mock.GetAgencies())
                .ReturnsAsync(new List<RosettaAgency>{expected})
                .Verifiable();

            // ACT
            var actionResult = await unitUnderTest.GetAgencies();

            // ASSERT
            rosettaStoneServiceMock.Verify();
            var result = actionResult.Result as OkObjectResult;
            Assert.IsNotNull(result);
            var rosettaFranchises = result.Value as IList<RosettaAgency>;
            Assert.IsNotNull(rosettaFranchises);
            Assert.AreEqual(expected.clear_care_agency, rosettaFranchises.First().clear_care_agency);
            Assert.AreEqual(expected.franchise_numbers[0], rosettaFranchises.First().franchise_numbers[0]);
            Assert.AreEqual(expected.franchise_numbers[1], rosettaFranchises.First().franchise_numbers[1]);
        }
    }
}