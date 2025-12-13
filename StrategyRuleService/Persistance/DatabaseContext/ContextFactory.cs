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

        // Bağlantı stringinizi buraya yazın
        optionsBuilder.UseSqlServer("server=MetropolTilkisi;database=RtdStartegyRule-Service;integrated security=SSPI;persist security info=False;Trusted_Connection=True;Encrypt=false");

        return new Context(optionsBuilder.Options);
    }

    
}
