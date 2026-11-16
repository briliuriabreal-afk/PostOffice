/**** Почтовое отделение: представления ****/
/*
 * Представления строятся одно на другом; рядом с расшифровками лежат FK-поля.
 * Столбцы перечисляются явно: представление с select * запоминает состав
 * столбцов и после изменения базового представления выдаёт данные не под теми именами.
 * После изменения этого скрипта перезапустить 004 и 006.
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание представлений ***';
go


-- работники: человек, роль, даты приёма и увольнения
create or alter view ViewEmployees as
select
    Employees.Id
    , Employees.PersonId
    , Employees.RoleId

    , People.Surname
    , People.[Name]
    , People.Patronymic
    , People.Passport
    , dbo.ShortName(People.Surname, People.[Name], People.Patronymic) as ShortName
    , Roles.[Name] as [Role]
    , Employees.HireDate
    , Employees.FireDate
from
    Employees join People on Employees.PersonId = People.Id
              join Roles  on Employees.RoleId = Roles.Id;
go


-- работающие сотрудники - единственное определение «работает»: нет даты увольнения
create or alter view ViewWorkingEmployees as
select
    ViewEmployees.Id
    , ViewEmployees.PersonId
    , ViewEmployees.RoleId

    , ViewEmployees.Surname
    , ViewEmployees.[Name]
    , ViewEmployees.Patronymic
    , ViewEmployees.Passport
    , ViewEmployees.ShortName
    , ViewEmployees.[Role]
    , ViewEmployees.HireDate
from
    ViewEmployees
where
    ViewEmployees.FireDate is null;
go


-- работающие почтальоны и число их участков
create or alter view ViewWorkingPostmen as
select
    ViewWorkingEmployees.Id
    , ViewWorkingEmployees.PersonId

    , ViewWorkingEmployees.Surname
    , ViewWorkingEmployees.ShortName
    , ViewWorkingEmployees.HireDate
    , (select count(*) from Districts where Districts.PostmanId = ViewWorkingEmployees.Id) as DistrictCount
from
    ViewWorkingEmployees
where
    ViewWorkingEmployees.[Role] = 'Почтальон';
go


-- участки и их почтальоны
create or alter view ViewDistricts as
select
    Districts.Id
    , Districts.PostmanId

    , Districts.Number
    , ViewEmployees.Surname   as PostmanSurname
    , ViewEmployees.ShortName as PostmanShortName
from
    Districts join ViewEmployees on Districts.PostmanId = ViewEmployees.Id;
go


-- дома: улица, номер, участок и почтальон
create or alter view ViewHouses as
select
    Houses.Id
    , Houses.StreetId
    , Houses.DistrictId
    , ViewDistricts.PostmanId

    , Streets.[Name] as Street
    , Houses.Building
    , ViewDistricts.Number as DistrictNumber
    , ViewDistricts.PostmanSurname
    , ViewDistricts.PostmanShortName
from
    Houses join Streets       on Houses.StreetId = Streets.Id
           join ViewDistricts on Houses.DistrictId = ViewDistricts.Id;
go


-- подписчики: человек, адрес, участок и почтальон
create or alter view ViewSubscribers as
select
    Subscribers.Id
    , Subscribers.PersonId
    , Subscribers.HouseId
    , ViewHouses.StreetId
    , ViewHouses.DistrictId
    , ViewHouses.PostmanId

    , People.Surname
    , People.[Name]
    , People.Patronymic
    , People.Passport
    , dbo.ShortName(People.Surname, People.[Name], People.Patronymic) as ShortName
    , ViewHouses.Street
    , ViewHouses.Building
    , Subscribers.Flat
    , dbo.FullAddress(ViewHouses.Street, ViewHouses.Building, Subscribers.Flat) as Address
    , ViewHouses.DistrictNumber
    , ViewHouses.PostmanSurname
    , ViewHouses.PostmanShortName
from
    Subscribers join People     on Subscribers.PersonId = People.Id
                join ViewHouses on Subscribers.HouseId = ViewHouses.Id;
go


-- каталог изданий с видом
create or alter view ViewPublications as
select
    Publications.Id
    , Publications.PublicationTypeId

    , Publications.PubIndex
    , Publications.Title
    , PublicationTypes.[Name] as PublicationType
    , Publications.Price
from
    Publications join PublicationTypes on Publications.PublicationTypeId = PublicationTypes.Id;
go


-- подписки - основа всех запросов и квитанции: подписчик, издание, оператор,
-- срок, дата окончания, цена на день оформления и стоимость
create or alter view ViewSubscriptions as
select
    Subscriptions.Id
    , Subscriptions.SubscriberId
    , Subscriptions.OperatorId
    , Subscriptions.PublicationId
    , ViewSubscribers.PersonId
    , ViewSubscribers.HouseId
    , ViewSubscribers.DistrictId
    , ViewSubscribers.PostmanId
    , ViewPublications.PublicationTypeId

    , ViewSubscribers.Surname
    , ViewSubscribers.[Name]
    , ViewSubscribers.Patronymic
    , ViewSubscribers.ShortName
    , ViewSubscribers.Address
    , ViewSubscribers.DistrictNumber
    , ViewSubscribers.PostmanShortName
    , ViewPublications.PubIndex
    , ViewPublications.Title
    , ViewPublications.PublicationType
    , ViewEmployees.ShortName as OperatorShortName
    , Subscriptions.IssueDate
    , Subscriptions.DateStart
    , dbo.SubscriptionEndDate(Subscriptions.DateStart, Subscriptions.Duration) as DateEnd
    , Subscriptions.Duration
    , Subscriptions.Copies
    , Subscriptions.Price
    , dbo.SubscriptionCost(Subscriptions.Price, Subscriptions.Duration, Subscriptions.Copies) as Cost
from
    Subscriptions join ViewSubscribers  on Subscriptions.SubscriberId = ViewSubscribers.Id
                  join ViewPublications on Subscriptions.PublicationId = ViewPublications.Id
                  join ViewEmployees    on Subscriptions.OperatorId = ViewEmployees.Id;
go


-- роли учётных записей: роль работника - из таблицы ролей, роль подписчика -
-- наличие строки в Subscribers; работник-подписчик получает две строки
create or alter view ViewAccountRoles as
select
    Accounts.PersonId
    , ViewWorkingEmployees.Id as EmployeeId
    , null as SubscriberId

    , Accounts.[Login]
    , ViewWorkingEmployees.ShortName
    , ViewWorkingEmployees.[Role]
from
    Accounts join ViewWorkingEmployees on Accounts.PersonId = ViewWorkingEmployees.PersonId
union all
select
    Accounts.PersonId
    , null
    , Subscribers.Id

    , Accounts.[Login]
    , dbo.ShortName(People.Surname, People.[Name], People.Patronymic)
    , 'Подписчик'
from
    Accounts join Subscribers on Accounts.PersonId = Subscribers.PersonId
             join People      on Accounts.PersonId = People.Id;
go

print '*** Завершено создание представлений ***';
go
