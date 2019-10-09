using System.Collections.Generic;
using System.Threading.Tasks;
using ClearCareOnline.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Rosetta.Models;
using Rosetta.Services;

namespace Rosetta.Controllers
{
    // todo: add in application insights
    // todo: add in logging where it makes sense
    // todo: create terraform to create app service, keyvault,
    // todo: update service to leverage keyvault
    // todo: commit to homeinstead repo, and remove temporary private repo
    // todo: create azure devops project
    // todo: add build and release pipelines
    // todo: add webhook to send email to pitcrew on failure and back for pro-active health notification

    [Authorize]
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        private readonly IRosettaStoneService _rosettaStoneService;

        public HomeController(IRosettaStoneService rosettaStoneService)
        {
            _rosettaStoneService = rosettaStoneService;
        }

        // GET status
        [ProducesResponseType(typeof(Status), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet]
        public async Task<ActionResult<Status>> Status()
        {
            var result = await _rosettaStoneService.GetStatus();
            return Ok(result);
        }

        // GET franchise/220
        [ProducesResponseType(typeof(RosettaFranchise), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpGet("franchise/{franchiseNumber}")]
        public async Task<ActionResult<RosettaFranchise>> GetFranchise(int franchiseNumber)
        {
            var result = await _rosettaStoneService.GetFranchise(franchiseNumber);
            if (result != null)
            {
                return Ok(result);
            }

            return NotFound();
        }

        // GET agencies
        [ProducesResponseType(typeof(IList<AgencyFranchiseMap>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet("agencies")]
        public async Task<ActionResult<IList<AgencyFranchiseMap>>> GetAgencies()
        {
            var result = await _rosettaStoneService.GetAgencies();
            return Ok(result);
        }

        // GET franchises
        [ProducesResponseType(typeof(IList<RosettaFranchise>), StatusCodes.Status200OK)]
        [Produces("application/json")]
        [HttpGet("franchises")]
        public async Task<ActionResult<IList<RosettaFranchise>>> GetFranchises()
        {
            var result = await _rosettaStoneService.GetFranchises();
            return Ok(result);
        }

        // note: this is here for debugging/testing purposes
        // GET franchises
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet("clear_cache")]
        public ActionResult ClearCache()
        {
            _rosettaStoneService.ClearCache();
            return Ok();
        }
    }
}
