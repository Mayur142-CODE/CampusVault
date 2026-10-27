using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace CampusVault.Services
{
    /// <summary>
    /// Single source for all dropdown data — departments, semesters, task types, divisions, academic years.
    /// </summary>
    public class LookupRepository
    {
        private readonly string _connectionString;

        public LookupRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["CampusVaultConnection"]?.ConnectionString;
        }

        public List<LookupItem> GetDepartments()
        {
            return GetList("SELECT DepartmentId AS Id, DepartmentName AS Name FROM Departments WHERE IsActive = 1 ORDER BY DepartmentName");
        }

        public List<LookupItem> GetSemesters()
        {
            return GetList("SELECT SemesterId AS Id, SemesterName AS Name FROM Semesters WHERE IsActive = 1 ORDER BY SemesterNumber");
        }

        public List<LookupItem> GetTaskTypes()
        {
            return GetList("SELECT TaskTypeId AS Id, TypeName AS Name FROM TaskTypes WHERE IsActive = 1 ORDER BY TypeName");
        }

        public List<LookupItem> GetDivisions()
        {
            return GetList("SELECT DivisionId AS Id, DivisionName AS Name FROM Divisions WHERE IsActive = 1 ORDER BY DivisionName");
        }

        public List<LookupItem> GetAcademicYears()
        {
            return GetList("SELECT AcademicYearId AS Id, YearName AS Name FROM AcademicYears WHERE IsActive = 1 ORDER BY YearName DESC");
        }

        public List<LookupItem> GetVerifiedInstitutions()
        {
            return GetList("SELECT InstitutionId AS Id, InstitutionName AS Name FROM Institutions WHERE IsVerified = 1 AND IsActive = 1 ORDER BY InstitutionName");
        }

        private List<LookupItem> GetList(string sql)
        {
            var list = new List<LookupItem>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new LookupItem
                        {
                            Id   = r["Id"].ToString(),
                            Name = r["Name"].ToString()
                        });
            }
            return list;
        }
    }

    public class LookupItem
    {
        public string Id   { get; set; }
        public string Name { get; set; }
    }
}
