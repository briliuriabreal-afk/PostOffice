/**** Почтовое отделение: табличные функции ****/
/*
 * Единственное определение «действующей подписки» на дату: SubscriptionsWithStatus.
 * Все запросы «на текущий момент» строятся через неё; приложение передаёт сегодняшнюю дату,
 * проверочные скрипты - любые даты.
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание табличных функций ***';
go


-- подписки, оформленные не позже заданной даты, и их состояние на эту дату:
-- действует, ожидает начала, завершена
create or alter function SubscriptionsWithStatus(@onDate date)
returns table
as
return
select
    ViewSubscriptions.Id
    , ViewSubscriptions.SubscriberId
    , ViewSubscriptions.OperatorId
    , ViewSubscriptions.PublicationId
    , ViewSubscriptions.PersonId
    , ViewSubscriptions.HouseId
    , ViewSubscriptions.DistrictId
    , ViewSubscriptions.PostmanId
    , ViewSubscriptions.PublicationTypeId

    , ViewSubscriptions.Surname
    , ViewSubscriptions.[Name]
    , ViewSubscriptions.Patronymic
    , ViewSubscriptions.ShortName
    , ViewSubscriptions.Address
    , ViewSubscriptions.DistrictNumber
    , ViewSubscriptions.PostmanShortName
    , ViewSubscriptions.PubIndex
    , ViewSubscriptions.Title
    , ViewSubscriptions.PublicationType
    , ViewSubscriptions.OperatorShortName
    , ViewSubscriptions.IssueDate
    , ViewSubscriptions.DateStart
    , ViewSubscriptions.DateEnd
    , ViewSubscriptions.Duration
    , ViewSubscriptions.Copies
    , ViewSubscriptions.Price
    , ViewSubscriptions.Cost
    , iif(@onDate between ViewSubscriptions.DateStart and ViewSubscriptions.DateEnd, 'действует',
        iif(@onDate < ViewSubscriptions.DateStart, 'ожидает начала', 'завершена')) as [Status]
from
    ViewSubscriptions
where
    ViewSubscriptions.IssueDate <= @onDate;
go


-- подписки, действующие на заданную дату
create or alter function ActiveSubscriptions(@onDate date)
returns table
as
return
select
    SubscriptionsWithStatus.Id
    , SubscriptionsWithStatus.SubscriberId
    , SubscriptionsWithStatus.OperatorId
    , SubscriptionsWithStatus.PublicationId
    , SubscriptionsWithStatus.PersonId
    , SubscriptionsWithStatus.HouseId
    , SubscriptionsWithStatus.DistrictId
    , SubscriptionsWithStatus.PostmanId
    , SubscriptionsWithStatus.PublicationTypeId

    , SubscriptionsWithStatus.Surname
    , SubscriptionsWithStatus.[Name]
    , SubscriptionsWithStatus.Patronymic
    , SubscriptionsWithStatus.ShortName
    , SubscriptionsWithStatus.Address
    , SubscriptionsWithStatus.DistrictNumber
    , SubscriptionsWithStatus.PostmanShortName
    , SubscriptionsWithStatus.PubIndex
    , SubscriptionsWithStatus.Title
    , SubscriptionsWithStatus.PublicationType
    , SubscriptionsWithStatus.DateStart
    , SubscriptionsWithStatus.DateEnd
    , SubscriptionsWithStatus.Duration
    , SubscriptionsWithStatus.Copies
from
    dbo.SubscriptionsWithStatus(@onDate) as SubscriptionsWithStatus
where
    SubscriptionsWithStatus.[Status] = 'действует';
go


-- сумма экземпляров действующих подписок по каждому участку с доставкой
create or alter function DistrictsCopies(@onDate date)
returns table
as
return
select
    ActiveSubscriptions.DistrictId
    , ActiveSubscriptions.DistrictNumber
    , ActiveSubscriptions.PostmanShortName
    , sum(ActiveSubscriptions.Copies) as Copies
from
    dbo.ActiveSubscriptions(@onDate) as ActiveSubscriptions
group by
    ActiveSubscriptions.DistrictId, ActiveSubscriptions.DistrictNumber, ActiveSubscriptions.PostmanShortName;
go

print '*** Завершено создание табличных функций ***';
go
