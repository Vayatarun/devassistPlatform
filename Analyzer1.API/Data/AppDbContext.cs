using Analyzer1.API.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Issue> Issues { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<Rule> Rules { get; set; }
}