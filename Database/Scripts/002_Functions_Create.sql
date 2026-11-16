/**** Почтовое отделение: скалярные функции ****/
/*
 * Каждая формула записана один раз и вызывается из представлений,
 * процедур и ограничений. Скалярная функция вызывается со схемой: dbo.Имя(...)
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание скалярных функций ***';
go


-- предел числа участков у одного почтальона
create or alter function MaxDistrictsPerPostman()
returns int
as
begin
    return 5;
end;
go


-- стоимость подписки: цена экземпляра за месяц * срок в месяцах * экземпляры
create or alter function SubscriptionCost(
    @price     float,     -- цена одного экземпляра за месяц
    @duration  int,       -- срок подписки в месяцах
    @copies    int        -- количество экземпляров
)
returns float
as
begin
    return @price * @duration * @copies;
end;
go


-- последний день подписки включительно
create or alter function SubscriptionEndDate(
    @dateStart  date,     -- начало подписки
    @duration   int       -- срок подписки в месяцах
)
returns date
as
begin
    return dateadd(day, -1, dateadd(month, @duration, @dateStart));
end;
go


-- «Фамилия И.О.», без отчества - «Фамилия И.»
create or alter function ShortName(
    @surname     nvarchar(50),
    @name        nvarchar(40),
    @patronymic  nvarchar(50)
)
returns nvarchar(60)
as
begin
    return @surname + ' ' + substring(@name, 1, 1) + '.'
        + iif(@patronymic is null, '', substring(@patronymic, 1, 1) + '.');
end;
go


-- адрес «ул. Ленина, д. 12, кв. 5», без квартиры - «ул. Ленина, д. 12»
create or alter function FullAddress(
    @street    nvarchar(60),
    @building  nvarchar(10),
    @flat      int
)
returns nvarchar(100)
as
begin
    return @street + ', д. ' + @building
        + iif(@flat is null, '', ', кв. ' + cast(@flat as nvarchar(10)));
end;
go


-- хеш пароля: SHA-256 от логина и пароля, логин уникален и работает как соль
create or alter function PasswordHash(
    @login     nvarchar(30),
    @password  nvarchar(50)
)
returns varbinary(32)
as
begin
    return hashbytes('SHA2_256', @login + @password);
end;
go

print '*** Завершено создание скалярных функций ***';
go
