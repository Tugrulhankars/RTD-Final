using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace Persistance.DatabaseContext;
public class ContextFactory : IDesignTimeDbContextFactory<Context>
{
    public Context CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<Context>();
        optionsBuilder.UseSqlServer("Server=localhost;Database=RtdStartegyRule-Service;Integrated Security=True;TrustServerCertificate=True;Encrypt=false");
        return new Context(optionsBuilder.Options);
    }
}
