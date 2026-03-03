using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System;

namespace Application.Tests.Common
{
    public abstract class ApplicationTestBase : IDisposable
    {
        protected readonly InsuranceDbContext Context;

        protected ApplicationTestBase()
        {
            var options = new DbContextOptionsBuilder<InsuranceDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            Context = new InsuranceDbContext(options);
            Context.Database.EnsureCreated();

            // Seed base data if needed, but EnsureCreated handles OnModelCreating seeds
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}
