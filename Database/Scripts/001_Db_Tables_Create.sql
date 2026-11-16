/**** Почтовое отделение: создание базы данных и таблиц ****/
/*
 * База данных хранит сведения о подписных изданиях, подписчиках, подписках,
 * работниках отделения и участках доставки. Таблицы создаются в порядке
 * зависимостей: сначала справочники и люди, затем то, что на них ссылается.
 * Повторный запуск безопасен: существующие база и таблицы не пересоздаются.
 */
use master;
go

if db_id('PostOffice_Soykin_PE521') is null begin
    create database PostOffice_Soykin_PE521 collate Cyrillic_General_CI_AS;
end;
go

use PostOffice_Soykin_PE521;
go

print '*** Создание таблиц базы данных PostOffice_Soykin_PE521 ***';


-- улицы, на которых стоят дома отделения; тип улицы входит в название
if object_id('Streets') is null begin
    create table Streets (
        Id      int          not null identity(1, 1),
        [Name]  nvarchar(60) not null,                  -- «ул. Ленина», «пр. Мира»

        constraint PK_Streets primary key (Id),
        constraint UQ_Streets_Name unique ([Name]),
        constraint CK_Streets_Name check ([Name] <> '')
    );
    print 'Таблица Streets создана';
end else
    print 'Таблица Streets уже создана';


-- роли работников: Заведующий, Оператор, Почтальон
if object_id('Roles') is null begin
    create table Roles (
        Id      int          not null identity(1, 1),
        [Name]  nvarchar(20) not null,

        constraint PK_Roles primary key (Id),
        constraint UQ_Roles_Name unique ([Name])
    );
    print 'Таблица Roles создана';
end else
    print 'Таблица Roles уже создана';


-- виды изданий: Газета, Журнал
if object_id('PublicationTypes') is null begin
    create table PublicationTypes (
        Id      int          not null identity(1, 1),
        [Name]  nvarchar(20) not null,

        constraint PK_PublicationTypes primary key (Id),
        constraint UQ_PublicationTypes_Name unique ([Name])
    );
    print 'Таблица PublicationTypes создана';
end else
    print 'Таблица PublicationTypes уже создана';


-- люди: общие персональные данные работников и подписчиков
if object_id('People') is null begin
    create table People (
        Id          int          not null identity(1, 1),
        Surname     nvarchar(50) not null,              -- фамилия
        [Name]      nvarchar(40) not null,              -- имя
        Patronymic  nvarchar(50) null,                  -- отчество, null - отчества нет
        Passport    nvarchar(15) not null,              -- серия и номер паспорта

        constraint PK_People primary key (Id),
        constraint UQ_People_Passport unique (Passport),
        constraint CK_People_Surname check (Surname <> ''),
        constraint CK_People_Name check ([Name] <> ''),
        constraint CK_People_Patronymic check (Patronymic <> ''),
        constraint CK_People_Passport check (Passport <> '')
    );
    print 'Таблица People создана';
end else
    print 'Таблица People уже создана';


-- работники отделения; пустая дата увольнения - человек работает
if object_id('Employees') is null begin
    create table Employees (
        Id        int  not null identity(1, 1),
        PersonId  int  not null,                        -- человек
        RoleId    int  not null,                        -- роль работника
        HireDate  date not null constraint DF_Employees_HireDate default getdate(),
        FireDate  date null,                            -- дата увольнения

        constraint PK_Employees primary key (Id),
        constraint UQ_Employees_PersonId unique (PersonId),
        constraint FK_Employees_People foreign key (PersonId) references People(Id),
        constraint FK_Employees_Roles foreign key (RoleId) references Roles(Id),
        constraint CK_Employees_HireDate check (HireDate <= getdate()),
        constraint CK_Employees_FireDate check (FireDate between HireDate and getdate())
    );
    print 'Таблица Employees создана';
end else
    print 'Таблица Employees уже создана';


-- участки доставки; у каждого участка всегда есть почтальон
if object_id('Districts') is null begin
    create table Districts (
        Id         int not null identity(1, 1),
        Number     int not null,                        -- номер участка
        PostmanId  int not null,                        -- почтальон, обслуживающий участок

        constraint PK_Districts primary key (Id),
        constraint UQ_Districts_Number unique (Number),
        constraint CK_Districts_Number check (Number > 0),
        constraint FK_Districts_Employees foreign key (PostmanId) references Employees(Id)
    );
    print 'Таблица Districts создана';
end else
    print 'Таблица Districts уже создана';


-- дома, которые обслуживает отделение; несколько домов образуют участок
if object_id('Houses') is null begin
    create table Houses (
        Id          int          not null identity(1, 1),
        StreetId    int          not null,              -- улица
        Building    nvarchar(10) not null,              -- номер дома: «12», «14а», «22к1»
        DistrictId  int          not null,              -- участок

        constraint PK_Houses primary key (Id),
        constraint UQ_Houses_StreetId_Building unique (StreetId, Building),
        constraint CK_Houses_Building check (Building <> ''),
        constraint FK_Houses_Streets foreign key (StreetId) references Streets(Id),
        constraint FK_Houses_Districts foreign key (DistrictId) references Districts(Id)
    );
    print 'Таблица Houses создана';
end else
    print 'Таблица Houses уже создана';


-- подписчики; домашний адрес - дом и квартира
if object_id('Subscribers') is null begin
    create table Subscribers (
        Id        int not null identity(1, 1),
        PersonId  int not null,                         -- человек
        HouseId   int not null,                         -- дом
        Flat      int null,                             -- квартира, null - частный дом

        constraint PK_Subscribers primary key (Id),
        constraint UQ_Subscribers_PersonId unique (PersonId),
        constraint CK_Subscribers_Flat check (Flat > 0),
        constraint FK_Subscribers_People foreign key (PersonId) references People(Id),
        constraint FK_Subscribers_Houses foreign key (HouseId) references Houses(Id)
    );
    print 'Таблица Subscribers создана';
end else
    print 'Таблица Subscribers уже создана';


-- учётные записи для входа в приложение; пароль хранится только хешем
if object_id('Accounts') is null begin
    create table Accounts (
        Id            int           not null identity(1, 1),
        PersonId      int           not null,           -- человек
        [Login]       nvarchar(30)  not null,           -- логин
        PasswordHash  varbinary(32) not null,           -- хеш SHA-256 логина и пароля

        constraint PK_Accounts primary key (Id),
        constraint UQ_Accounts_PersonId unique (PersonId),
        constraint UQ_Accounts_Login unique ([Login]),
        constraint CK_Accounts_Login check ([Login] <> ''),
        constraint FK_Accounts_People foreign key (PersonId) references People(Id)
    );
    print 'Таблица Accounts создана';
end else
    print 'Таблица Accounts уже создана';


-- каталог подписных изданий
if object_id('Publications') is null begin
    create table Publications (
        Id                 int           not null identity(1, 1),
        PubIndex           nvarchar(12)  not null,      -- подписной индекс
        Title              nvarchar(100) not null,      -- название
        PublicationTypeId  int           not null,      -- вид издания
        Price              float         not null,      -- цена одного экземпляра за месяц

        constraint PK_Publications primary key (Id),
        constraint UQ_Publications_PubIndex unique (PubIndex),
        constraint CK_Publications_PubIndex check (PubIndex <> ''),
        constraint CK_Publications_Title check (Title <> ''),
        constraint CK_Publications_Price check (Price > 0),
        constraint FK_Publications_PublicationTypes foreign key (PublicationTypeId)
            references PublicationTypes(Id)
    );
    print 'Таблица Publications создана';
end else
    print 'Таблица Publications уже создана';


-- подписки; одна подписка - одно издание, она же квитанция (номер квитанции - Id)
if object_id('Subscriptions') is null begin
    create table Subscriptions (
        Id             int   not null identity(1, 1),
        SubscriberId   int   not null,                  -- подписчик
        OperatorId     int   not null,                  -- работник, оформивший подписку
        PublicationId  int   not null,                  -- издание
        IssueDate      date  not null constraint DF_Subscriptions_IssueDate default getdate(),
        DateStart      date  not null,                  -- начало подписки, 1-е число месяца
        Duration       int   not null,                  -- срок подписки в месяцах
        Copies         int   not null,                  -- количество экземпляров
        Price          float not null,                  -- цена экземпляра за месяц на день оформления

        constraint PK_Subscriptions primary key (Id),
        constraint UQ_Subscriptions_SubscriberId_PublicationId_DateStart
            unique (SubscriberId, PublicationId, DateStart),
        constraint CK_Subscriptions_IssueDate check (IssueDate <= getdate()),
        constraint CK_Subscriptions_DateStart check (
            datepart(day, DateStart) = 1
            and DateStart between dateadd(day, 1, IssueDate) and dateadd(month, 12, IssueDate)
        ),
        constraint CK_Subscriptions_Duration check (Duration between 1 and 12),
        constraint CK_Subscriptions_Copies check (Copies > 0),
        constraint CK_Subscriptions_Price check (Price > 0),
        constraint FK_Subscriptions_Subscribers foreign key (SubscriberId) references Subscribers(Id),
        constraint FK_Subscriptions_Employees foreign key (OperatorId) references Employees(Id),
        constraint FK_Subscriptions_Publications foreign key (PublicationId) references Publications(Id)
    );
    print 'Таблица Subscriptions создана';
end else
    print 'Таблица Subscriptions уже создана';
go

print '*** Завершено создание таблиц базы данных ***';
go
