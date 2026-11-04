using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
namespace samane.Data
{
    public interface ISqlConnectionFactory
    {
        SqlConnection Create();
    }
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string connectionString;
        public SqlConnectionFactory(IConfiguration config)
        {
            connectionString = config.GetConnectionString("PersonnelReview");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new System.InvalidOperationException("ConnectionStrings:PersonnelReview is required.");
            var settings = new SqlConnectionStringBuilder(connectionString);
            if (settings.IntegratedSecurity &&
                (!string.IsNullOrEmpty(settings.UserID) || !string.IsNullOrEmpty(settings.Password)))
                throw new System.InvalidOperationException(
                    "Choose Windows authentication OR SQL credentials. For SQL login use Integrated Security=false.");
        }
        public SqlConnection Create()
        {
            return new SqlConnection(connectionString);
        }
    }
}
