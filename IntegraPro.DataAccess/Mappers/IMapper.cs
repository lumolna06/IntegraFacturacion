using System.Data;
using Microsoft.Data.SqlClient;

namespace IntegraPro.DataAccess.Mappers;

public interface IMapper<T>
{
    T MapFromRow(DataRow row);
    SqlParameter[] MapToParameters(T entity);
}