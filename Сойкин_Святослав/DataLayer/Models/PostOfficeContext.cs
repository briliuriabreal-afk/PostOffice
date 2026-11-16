using DataLayer.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace DataLayer.Models;

// Контекст EF Core: справочники, каталог изданий и подписчики. EF пишет только
// в таблицы, правила которых касаются одной таблицы: издания и адреса подписчиков.
// Всё, где правило охватывает несколько таблиц, выполняют хранимые процедуры через ADO.NET
public class PostOfficeContext : DbContext
{
    public DbSet<Street> Streets => Set<Street>();
    public DbSet<House> Houses => Set<House>();
    public DbSet<PublicationType> PublicationTypes => Set<PublicationType>();
    public DbSet<Publication> Publications => Set<Publication>();
    public DbSet<Person> People => Set<Person>();
    public DbSet<Subscriber> Subscribers => Set<Subscriber>();


    // База создаётся скриптами вместе с представлениями, функциями, процедурами
    // и триггерами, поэтому EnsureCreated и EnsureDeleted не вызываются:
    // EnsureDeleted удалил бы базу вместе с процедурами, EnsureCreated на пустом
    // сервере создал бы только таблицы со своими именами ограничений
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlServer(Connection.ConnectionString);
    } // OnConfiguring


    // соответствие классов таблицам базы данных
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        // улицы
        var entityStreet = modelBuilder.Entity<Street>();
        entityStreet.ToTable("Streets");
        entityStreet.Property(street => street.Name).IsRequired().HasMaxLength(60);
        entityStreet.HasIndex(street => street.Name).IsUnique().HasDatabaseName("UQ_Streets_Name");

        // дома
        var entityHouse = modelBuilder.Entity<House>();
        entityHouse.ToTable("Houses");
        entityHouse.Property(house => house.Building).IsRequired().HasMaxLength(10);
        entityHouse
            .HasOne(house => house.Street)
            .WithMany(street => street.Houses)
            .HasForeignKey(house => house.StreetId)
            .OnDelete(DeleteBehavior.NoAction);

        // виды изданий
        var entityPublicationType = modelBuilder.Entity<PublicationType>();
        entityPublicationType.ToTable("PublicationTypes");
        entityPublicationType.Property(type => type.Name).IsRequired().HasMaxLength(20);

        // каталог изданий
        var entityPublication = modelBuilder.Entity<Publication>();
        entityPublication.ToTable("Publications");
        entityPublication.Property(publication => publication.PubIndex).IsRequired().HasMaxLength(12);
        entityPublication.Property(publication => publication.Title).IsRequired().HasMaxLength(100);
        entityPublication
            .HasIndex(publication => publication.PubIndex)
            .IsUnique()
            .HasDatabaseName("UQ_Publications_PubIndex");
        entityPublication
            .HasOne(publication => publication.PublicationType)
            .WithMany(type => type.Publications)
            .HasForeignKey(publication => publication.PublicationTypeId)
            .OnDelete(DeleteBehavior.NoAction);

        // люди
        var entityPerson = modelBuilder.Entity<Person>();
        entityPerson.ToTable("People");
        entityPerson.Property(person => person.Surname).IsRequired().HasMaxLength(50);
        entityPerson.Property(person => person.Name).IsRequired().HasMaxLength(40);
        entityPerson.Property(person => person.Patronymic).HasMaxLength(50);
        entityPerson.Property(person => person.Passport).IsRequired().HasMaxLength(15);
        entityPerson.HasIndex(person => person.Passport).IsUnique().HasDatabaseName("UQ_People_Passport");

        entityPerson.Property(person => person.Login).IsRequired().HasMaxLength(30);
        entityPerson.Property(person => person.PasswordHash).IsRequired().HasMaxLength(32);
        entityPerson.HasIndex(person => person.Login).IsUnique().HasDatabaseName("UQ_People_Login");

        // подписчики: связь 1:1 с человеком, ключ держит подписчик
        var entitySubscriber = modelBuilder.Entity<Subscriber>();
        entitySubscriber.ToTable("Subscribers");
        entitySubscriber
            .HasOne(subscriber => subscriber.Person)
            .WithOne()
            .HasForeignKey<Subscriber>(subscriber => subscriber.PersonId)
            .OnDelete(DeleteBehavior.NoAction);
        entitySubscriber
            .HasOne(subscriber => subscriber.House)
            .WithMany()
            .HasForeignKey(subscriber => subscriber.HouseId)
            .OnDelete(DeleteBehavior.NoAction);
    } // OnModelCreating

} // class PostOfficeContext
