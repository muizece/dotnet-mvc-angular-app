using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

[ApiController]
[Route("api/[controller]")]
public class RecordsController : ControllerBase
{
    private readonly string _connectionString = "data source=DESKTOP-45QI8CF;initial catalog=StoreDB;integrated security=true;encrypt=false";

        [HttpGet("{id}")]
        public IActionResult GetCoiRecordDetails(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("GetCoiRecordDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@CoiRecordId", id);

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                            return NotFound(new { message = "CoiRecord not found." });

                        // Read CoiRecords
                        var record = new
                        {
                            Name = "",
                            Position = "",
                            StaffId = "",
                            Subsidiaries = "",
                            companyDetails = new List<dynamic>(),
                            relativeDetails = new List<dynamic>()
                        };

                        if (reader.Read())
                        {
                            record = new
                            {
                                Name = reader["Name"].ToString(),
                                Position = reader["Position"].ToString(),
                                StaffId = reader["StaffId"].ToString(),
                                Subsidiaries = reader["Subsidiaries"].ToString(),
                                companyDetails = new List<dynamic>(),
                                relativeDetails = new List<dynamic>()
                            };
                        }

                        // Read CompanyDetails
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                record.companyDetails.Add(new
                                {
                                    CompanyName = reader["CompanyName"].ToString(),
                                    OwnerName = reader["OwnerName"].ToString(),
                                    CR = reader["CR"].ToString(),
                                    CNin = reader["CNin"].ToString(),
                                    Cshares = reader["CShares"].ToString()
                                });
                            }
                        }

                        // Read RelativesDetails
                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                record.relativeDetails.Add(new
                                {
                                    RelativeName = reader["RelativeName"].ToString(),
                                    Relation = reader["Relation"].ToString(),
                                    IdNumber = reader["IdNumber"].ToString(),
                                    RNin = reader["RNin"].ToString(),
                                    Rshares = reader["RShares"].ToString()
                                });
                            }
                        }

                        return Ok(record);
                    }
                }
            }
        }


        [HttpPost]
        public IActionResult Post([FromBody] Record record)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var relativeDetailsTable = new DataTable();
                relativeDetailsTable.Columns.Add("RelativeName", typeof(string));
                relativeDetailsTable.Columns.Add("Relation", typeof(string));
                relativeDetailsTable.Columns.Add("IdNumber", typeof(string));
                relativeDetailsTable.Columns.Add("RNin", typeof(string));
                relativeDetailsTable.Columns.Add("RShares", typeof(string));

                foreach (var relative in record.RelativeDetails)
                {
                    relativeDetailsTable.Rows.Add(relative.RelativeName, relative.Relation, relative.IdNumber, relative.RNin, relative.RShares);
                }

                var companyDetailsTable = new DataTable();
                companyDetailsTable.Columns.Add("CompanyName", typeof(string));
                companyDetailsTable.Columns.Add("OwnerName", typeof(string));
                companyDetailsTable.Columns.Add("CR", typeof(string));
                companyDetailsTable.Columns.Add("CNin", typeof(string));
                companyDetailsTable.Columns.Add("CShares", typeof(string));

                foreach (var company in record.CompanyDetails)
                {
                    companyDetailsTable.Rows.Add(company.CompanyName, company.OwnerName, company.CR, company.CNin, company.CShares);
                }

                // Use stored procedure
                using (var command = new SqlCommand("InsertCoiRecord", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters for the stored procedure
                    command.Parameters.AddWithValue("@StaffId", record.StaffId);
                    command.Parameters.AddWithValue("@Name", record.Name);
                    command.Parameters.AddWithValue("@Position", record.Position);
                    command.Parameters.AddWithValue("@Subsidiaries", record.Subsidiaries);

                    int currentYear = DateTime.Now.Year;
                    command.Parameters.AddWithValue("@SubmissionYear", currentYear);
                    // Add table-valued parameters
                    SqlParameter tvpRelatives = new SqlParameter("@RelativeDetails", SqlDbType.Structured)
                    {
                        TypeName = "dbo.RelativeDetailsType",
                        Value = relativeDetailsTable
                    };
                    command.Parameters.Add(tvpRelatives);

                    SqlParameter tvpCompanies = new SqlParameter("@CompanyDetails", SqlDbType.Structured)
                    {
                        TypeName = "dbo.CompanyDetailsType",
                        Value = companyDetailsTable
                    };
                    command.Parameters.Add(tvpCompanies);

                    // Execute stored procedure and capture the result
                    var result = command.ExecuteScalar();

                    if ((int)result == -1)
                    {
                        // Duplicate StaffId
                        return Ok(new { message = "Duplicate record found for this Staff ID in the current year." });
                    }

                    // Success
                    return Ok(new { message = "Record and associated data inserted successfully.", newRecordId = result });
                }
            }
        }

        [HttpGet("GetTradesByYear/{year}")]
        public IActionResult GetTradesByYear(int year)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand("GetTradesByYear", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SubmissionYear", year);

                    using (var reader = command.ExecuteReader())
                    {
                        var trades = new List<dynamic>();
                        while (reader.Read())
                        {
                            trades.Add(new
                            {
                                StaffId = reader["StaffId"].ToString(),
                                Name = reader["Name"].ToString(),
                                Position = reader["Position"].ToString(),
                                Subsidiaries = reader["Subsidiaries"].ToString()
                            });
                        }
                        return Ok(trades);
                    }
                }
            }
        }

}


public class Record
{
    //validations needed on length
    public int Id { get; set; }
    public int StaffId { get; set; }
    public string Name { get; set; }
    public string Position { get; set; }
    public string Subsidiaries { get; set; }
    public List<RelativeDetail> RelativeDetails { get; set; }
    public List<CompanyDetail> CompanyDetails { get; set; }
}

public class RelativeDetail
{
    public int Id { get; set; }
    public string RelativeName { get; set; }
    public string Relation { get; set; }
    public string IdNumber { get; set; }
    public string RNin { get; set; }
    public string RShares { get; set; }
}

public class CompanyDetail
{
    public int Id { get; set; }
    public string CompanyName { get; set; }
    public string OwnerName { get; set; }
    public string CR { get; set; }
    public string CNin { get; set; }
    public string CShares { get; set; }
}

