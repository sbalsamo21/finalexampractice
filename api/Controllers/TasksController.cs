using Microsoft.AspNetCore.Mvc;
using MyApp.Namespace.DataAccess;
using MyApp.Namespace.Models;
using MySqlConnector;
using System.Data;

namespace MyApp.Namespace
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly Database _db;

        public TasksController(Database db)
        {
            _db = db;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItem>>> GetAllTasks()
        {
            try
            {
                const string query = "SELECT taskid, name, description, status, created_at, updated_at FROM tasks ORDER BY created_at DESC";
                var dataTable = await _db.ExecuteQueryAsync(query);

                var tasks = new List<TaskItem>();
                foreach (DataRow row in dataTable.Rows)
                {
                    tasks.Add(MapRowToTaskItem(row));
                }

                return Ok(tasks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving tasks", error = ex.Message });
            }
        }

        // GET: api/tasks/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetTask(int id)
        {
            try
            {
                const string query = "SELECT taskid, name, description, status, created_at, updated_at FROM tasks WHERE taskid = @id";
                var parameters = new[]
                {
                    new MySqlParameter("@id", id)
                };

                var dataTable = await _db.ExecuteQueryAsync(query, parameters);

                if (dataTable.Rows.Count == 0)
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                var task = MapRowToTaskItem(dataTable.Rows[0]);
                return Ok(task);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the task", error = ex.Message });
            }
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskItem>> CreateTask([FromBody] TaskItem task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                return BadRequest(new { message = "Task name is required" });
            }

            var validStatuses = new[] { "Not Started", "In Progress", "Completed" };
            if (!string.IsNullOrEmpty(task.Status) && !validStatuses.Contains(task.Status))
            {
                return BadRequest(new { message = $"Status must be one of: {string.Join(", ", validStatuses)}" });
            }

            try
            {
                const string insertQuery = @"
                    INSERT INTO tasks (name, description, status, created_at, updated_at)
                    VALUES (@name, @description, @status, NOW(), NOW())";

                var status = string.IsNullOrWhiteSpace(task.Status) ? "Not Started" : task.Status;

                var insertParams = new[]
                {
                    new MySqlParameter("@name", task.Name),
                    new MySqlParameter("@description", (object?)task.Description ?? DBNull.Value),
                    new MySqlParameter("@status", status)
                };

                var newTaskId = await _db.ExecuteInsertAndGetIdAsync(insertQuery, insertParams);

                const string selectQuery = "SELECT taskid, name, description, status, created_at, updated_at FROM tasks WHERE taskid = @id";
                var selectParams = new[] { new MySqlParameter("@id", (int)newTaskId) };
                var dataTable = await _db.ExecuteQueryAsync(selectQuery, selectParams);
                var createdTask = MapRowToTaskItem(dataTable.Rows[0]);

                return CreatedAtAction(nameof(GetTask), new { id = createdTask.TaskId }, createdTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the task", error = ex.Message });
            }
        }

        // PUT: api/tasks/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItem>> UpdateTask(int id, [FromBody] TaskItem task)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                return BadRequest(new { message = "Task name is required" });
            }

            var validStatuses = new[] { "Not Started", "In Progress", "Completed" };
            if (!string.IsNullOrEmpty(task.Status) && !validStatuses.Contains(task.Status))
            {
                return BadRequest(new { message = $"Status must be one of: {string.Join(", ", validStatuses)}" });
            }

            try
            {
                // Check if task exists
                const string checkQuery = "SELECT COUNT(*) FROM tasks WHERE taskid = @id";
                var checkParams = new[] { new MySqlParameter("@id", id) };
                var exists = await _db.ExecuteScalarAsync<int>(checkQuery, checkParams) > 0;

                if (!exists)
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                const string updateQuery = @"
                    UPDATE tasks
                    SET name = @name,
                        description = @description,
                        status = @status,
                        updated_at = NOW()
                    WHERE taskid = @id";

                var parameters = new[]
                {
                    new MySqlParameter("@id", id),
                    new MySqlParameter("@name", task.Name),
                    new MySqlParameter("@description", (object?)task.Description ?? DBNull.Value),
                    new MySqlParameter("@status", task.Status ?? "Not Started")
                };

                await _db.ExecuteNonQueryAsync(updateQuery, parameters);

                const string selectQuery = "SELECT taskid, name, description, status, created_at, updated_at FROM tasks WHERE taskid = @id";
                var selectParams = new[] { new MySqlParameter("@id", id) };
                var dataTable = await _db.ExecuteQueryAsync(selectQuery, selectParams);
                var updatedTask = MapRowToTaskItem(dataTable.Rows[0]);

                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the task", error = ex.Message });
            }
        }

        // DELETE: api/tasks/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                // Check if task exists
                const string checkQuery = "SELECT COUNT(*) FROM tasks WHERE taskid = @id";
                var checkParams = new[] { new MySqlParameter("@id", id) };
                var exists = await _db.ExecuteScalarAsync<int>(checkQuery, checkParams) > 0;

                if (!exists)
                {
                    return NotFound(new { message = $"Task with id {id} not found" });
                }

                const string deleteQuery = "DELETE FROM tasks WHERE taskid = @id";
                var parameters = new[] { new MySqlParameter("@id", id) };
                await _db.ExecuteNonQueryAsync(deleteQuery, parameters);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the task", error = ex.Message });
            }
        }

        private TaskItem MapRowToTaskItem(DataRow row)
        {
            return new TaskItem
            {
                TaskId = Convert.ToInt32(row["taskid"]),
                Name = row["name"].ToString() ?? string.Empty,
                Description = row["description"] == DBNull.Value ? null : row["description"].ToString(),
                Status = row["status"].ToString() ?? "Not Started",
                CreatedAt = row["created_at"] != DBNull.Value ? Convert.ToDateTime(row["created_at"]) : DateTime.MinValue,
                UpdatedAt = row["updated_at"] != DBNull.Value ? Convert.ToDateTime(row["updated_at"]) : DateTime.MinValue
            };
        }
    }
}

