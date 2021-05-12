using BlobRepositoryDemo.Server.Data;
using BlobRepositoryDemo.Shared.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlobRepositoryDemo.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CustomerWithGuidIdsController : ControllerBase
    {
        IRepository<CustomerWithGuidId> customersManager;

        public CustomerWithGuidIdsController(IRepository<CustomerWithGuidId> _customersManager)
        {
            customersManager = _customersManager;
        }

        [HttpGet("deleteall")]
        public async Task<ActionResult> DeleteAll()
        {
            try
            {
                await customersManager.DeleteAll();
                return NoContent();
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIListOfEntityResponse<CustomerWithGuidId>>> Get()
        {
            try
            {
                var result = await customersManager.Get();
                return Ok(new APIListOfEntityResponse<CustomerWithGuidId>()
                {
                    Success = true,
                    Data = result
                });
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpGet("{Id}")]
        public async Task<ActionResult<APIEntityResponse<CustomerWithGuidId>>> GetById(Guid Id)
        {

            try
            {
                var result = await customersManager.GetById(Id);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Customer Not Found" },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPost]
        public async Task<ActionResult<APIEntityResponse<CustomerWithGuidId>>> Insert([FromBody] CustomerWithGuidId Customer)
        {
            try
            {
                var result = await customersManager.Insert(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Could not find customer after adding it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }
        }

        [HttpPut]
        public async Task<ActionResult<APIEntityResponse<CustomerWithGuidId>>> Update([FromBody] CustomerWithGuidId Customer)
        {
            try
            {
                var result = await customersManager.Update(Customer);
                if (result != null)
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = true,
                        Data = result
                    });
                }
                else
                {
                    return Ok(new APIEntityResponse<CustomerWithGuidId>()
                    {
                        Success = false,
                        ErrorMessages = new List<string>() { "Could not find customer after updating it." },
                        Data = null
                    });
                }
            }
            catch (Exception ex)
            {
                // log exception here
                return StatusCode(500);
            }

        }

        [HttpDelete("{Id}")]
        public async Task<ActionResult<bool>> Delete(Guid Id)
        {
            try
            {
                return await customersManager.Delete(Id);
            }
            catch (Exception ex)
            {
                // log exception here
                var msg = ex.Message;
                return StatusCode(500);
            }
        }

    }
}
