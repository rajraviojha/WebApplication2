using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace WebApplication2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TodoAppController : ControllerBase, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly SqlConnection _connection;

        public TodoAppController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connection = new SqlConnection(_configuration.GetConnectionString("todoAppDBCon"));
        }

        [HttpGet("GetNotes")]
        public async Task<IActionResult> GetNotes()
        {
            try
            {
                await _connection.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand("SELECT * FROM dbo.Notes", _connection))
                using (SqlDataReader myReader = await myCommand.ExecuteReaderAsync())
                {
                    DataTable table = new DataTable();
                    table.Load(myReader);
                    return Ok(table);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            finally
            {
                _connection.Close();
            }
        }

        [HttpPost("AddNotes")]
        public async Task<IActionResult> AddNotes([FromBody] JObject requestBody)
        {
            try
            {
                if (requestBody == null || !requestBody.ContainsKey("newNote"))
                {
                    return BadRequest("The 'newNote' field is required.");
                }

                string newNote = requestBody["newNote"].ToString();

                if (string.IsNullOrEmpty(newNote))
                {
                    return BadRequest("The 'newNote' field cannot be empty.");
                }

                await _connection.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand("INSERT INTO dbo.Notes (NoteText) VALUES (@newNote)", _connection))
                {
                    myCommand.Parameters.AddWithValue("@newNote", newNote);
                    await myCommand.ExecuteNonQueryAsync();
                }
                return Ok("Note added successfully.");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 207) // Check for specific error code for invalid column name
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Invalid column name 'NoteText'. Details: {ex.Message}");
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, $"Database error: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, $"An unexpected error occurred: {ex.Message}");
            }
            finally
            {
                _connection.Close();
            }
        }


        [HttpDelete("DeleteNotes/{id}")]
        public async Task<IActionResult> DeleteNotes(int id)
        {
            try
            {
                await _connection.OpenAsync();
                using (SqlCommand myCommand = new SqlCommand("DELETE FROM dbo.Notes WHERE Id = @id", _connection))
                {
                    myCommand.Parameters.AddWithValue("@id", id);
                    await myCommand.ExecuteNonQueryAsync();
                }
                return Ok("Note deleted successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ex.Message);
            }
            finally
            {
                _connection.Close();
            }
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
