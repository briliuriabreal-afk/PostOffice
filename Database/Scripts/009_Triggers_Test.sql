/**** Почтовое отделение: проверка триггеров и заполнения ****/
/*
 * Запускать после заполнения (005) и создания триггеров (008).
 * Каждая проверка - отдельный пакет: откат в триггере прерывает пакет целиком.
 * Проверки «пройдет» меняют данные: после скрипта перезапустить 005 и 008.
 */
use PostOffice_Soykin_PE521;
go


/**** Контрольные выборки по заполнению - каждая должна быть пустой ****/
-- триггеры создаются после заполнения, поэтому данные заполнения проверяются здесь

-- пересечения подписок одного подписчика на одно издание
select
    First.Id
    , Second.Id as OtherId
from
    ViewSubscriptions as First join ViewSubscriptions as Second
        on First.SubscriberId = Second.SubscriberId
        and First.PublicationId = Second.PublicationId
        and First.Id < Second.Id
where
    First.DateStart between Second.DateStart and Second.DateEnd
    or Second.DateStart between First.DateStart and First.DateEnd;

-- участки, закреплённые не за работающим почтальоном
select
    Districts.Number
from
    Districts
where
    Districts.PostmanId not in (select ViewWorkingPostmen.Id from ViewWorkingPostmen);

-- почтальоны с числом участков больше предела
select
    ViewWorkingPostmen.ShortName
    , ViewWorkingPostmen.DistrictCount
from
    ViewWorkingPostmen
where
    ViewWorkingPostmen.DistrictCount > dbo.MaxDistrictsPerPostman();

-- подписки, оформленные не оператором и не заведующим
select
    ViewSubscriptions.Id
from
    ViewSubscriptions join ViewEmployees on ViewSubscriptions.OperatorId = ViewEmployees.Id
where
    ViewEmployees.[Role] not in ('Оператор', 'Заведующий');
go


-- Таблица УЧАСТКИ -------------------------------------

-- этот запрос пройдет: участок 10 Николаева - Смирновой (у неё станет 3)
update
    Districts
set
    PostmanId = 5
where
    Districts.Id = 10;
go

-- этот запрос не пройдет: шестой участок Петрову
update
    Districts
set
    PostmanId = 4
where
    Districts.Id = 6;
go

-- этот запрос не пройдет: участок уволенной Фёдоровой
update
    Districts
set
    PostmanId = 8
where
    Districts.Id = 7;
go

-- этот запрос не пройдет: участок оператору Козловой
update
    Districts
set
    PostmanId = 2
where
    Districts.Id = 7;
go

-- этот запрос не пройдет: четыре участка разом Васильеву (у него станет 6)
update
    Districts
set
    PostmanId = 6
where
    Districts.Id in (1, 2, 6, 7);
go

-- этот запрос пройдет: два новых участка Васильеву (у него станет 4)
insert into Districts
    (Number, PostmanId)
values
    (11, 6),
    (12, 6);
go

-- этот запрос не пройдет: новый участок уволенной Фёдоровой
insert into Districts
    (Number, PostmanId)
values
    (13, 8);
go

-- этот запрос не пройдет: новый участок Петрову, у него уже 5
insert into Districts
    (Number, PostmanId)
values
    (13, 4);
go

-- Таблица УЧАСТКИ -------------------------------------


-- Таблица РАБОТНИКИ -------------------------------------

-- этот запрос не пройдет: увольнение Петрова, за ним участки
update
    Employees
set
    FireDate = getdate()
where
    Employees.Id = 4;
go

-- этот запрос не пройдет: почтальон Смирнова становится оператором, за ней участки
update
    Employees
set
    RoleId = 2
where
    Employees.Id = 5;
go

-- этот запрос не пройдет: увольнение двух почтальонов с участками разом
update
    Employees
set
    FireDate = getdate()
where
    Employees.Id in (6, 7);
go

-- этот запрос пройдет: увольнение оператора Морозова, участков у него нет
update
    Employees
set
    FireDate = getdate()
where
    Employees.Id = 3;
go

-- Таблица РАБОТНИКИ -------------------------------------


-- Таблица ПОДПИСКИ -------------------------------------

-- этот запрос пройдет: Орлова выписывает АиФ со следующего месяца
declare @nextMonth date = dateadd(month, 1, dateadd(day, 1 - datepart(day, getdate()), getdate()));
insert into Subscriptions
    (SubscriberId, OperatorId, PublicationId, DateStart, Duration, Copies, Price)
values
    (7, 2, 2, @nextMonth, 6, 1, 98.5);
go

-- этот запрос не пройдет: у Кузнецова КП и так действует до 31.03.2027
declare @nextMonth date = dateadd(month, 1, dateadd(day, 1 - datepart(day, getdate()), getdate()));
insert into Subscriptions
    (SubscriberId, OperatorId, PublicationId, DateStart, Duration, Copies, Price)
values
    (1, 2, 1, @nextMonth, 3, 1, 120);
go

-- этот запрос не пройдет: две новые подписки Орловой на Российскую газету пересекаются между собой
declare @nextMonth date = dateadd(month, 1, dateadd(day, 1 - datepart(day, getdate()), getdate()));
insert into Subscriptions
    (SubscriberId, OperatorId, PublicationId, DateStart, Duration, Copies, Price)
values
    (7, 2, 3, @nextMonth, 3, 1, 140),
    (7, 2, 3, dateadd(month, 1, @nextMonth), 3, 1, 140);
go

-- этот запрос не пройдет: подписку оформляет почтальон Петров
declare @nextMonth date = dateadd(month, 1, dateadd(day, 1 - datepart(day, getdate()), getdate()));
insert into Subscriptions
    (SubscriberId, OperatorId, PublicationId, DateStart, Duration, Copies, Price)
values
    (7, 4, 6, @nextMonth, 3, 1, 75);
go

-- этот запрос не пройдет: изменение количества экземпляров в выданной квитанции
update
    Subscriptions
set
    Copies = 5
where
    Subscriptions.Id = 1;
go

-- этот запрос не пройдет: изменение цены в выданной квитанции
update
    Subscriptions
set
    Price = 1
where
    Subscriptions.Id = 1;
go

-- этот запрос не пройдет: изменение срока и цены сразу во всех квитанциях Кузнецова
update
    Subscriptions
set
    Duration = 1
    , Price = 1
where
    Subscriptions.SubscriberId = 1;
go

-- этот запрос не пройдет: удаление выданной квитанции
delete from
    Subscriptions
where
    Subscriptions.Id = 14;
go

-- этот запрос не пройдет: удаление всех квитанций 2024 и 2025 годов
delete from
    Subscriptions
where
    Subscriptions.IssueDate between '2024-01-01' and '2025-12-31';
go

-- Таблица ПОДПИСКИ -------------------------------------


-- итог: сколько подписок и участков осталось - удаления и изменения не прошли
select
    (select count(*) from Subscriptions) as SubscriptionsCount
    , (select count(*) from Districts) as DistrictsCount
    , (select count(*) from Subscriptions where Copies = 5 or Price = 1) as ChangedSubscriptions;
go
