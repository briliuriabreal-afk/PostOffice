/**** Почтовое отделение: хранимые процедуры ****/
/*
 * Выборки: запросы 1-6, справка, отчёт о доставке, кабинеты подписчика и почтальона, квитанция.
 * Процедуры «на дату» принимают @onDate; null - сегодняшняя дата.
 *
 * Операции: регистрация, учётные записи, вход, оформление подписки, приём,
 * передача участка, увольнение. Ошибку пользователя операция сообщает фразой
 * в выходном параметре @message и ничего не записывает; null - операция выполнена.
 * Новый Id находится по естественному ключу: человек - по паспорту,
 * работник, подписчик, учётная запись - по PersonId, подписка - по подписчику,
 * изданию и началу.
 */
use PostOffice_Soykin_PE521;
go

print '*** Создание хранимых процедур ***';
go


/**** Выборки ****/

-- Запрос 1. Наименование и количество экземпляров всех изданий, получаемых отделением:
-- «всех» - левое соединение от каталога, у невыписанных изданий 0;
-- условие «действует на дату» стоит внутри функции, поэтому левое соединение не схлопывается
create or alter proc PublicationsCopies
    @onDate date = null         -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        ViewPublications.Id
        , ViewPublications.PubIndex
        , ViewPublications.Title
        , ViewPublications.PublicationType
        , isnull(sum(ActiveSubscriptions.Copies), 0) as Copies
    from
        ViewPublications left join dbo.ActiveSubscriptions(@onDate) as ActiveSubscriptions
            on ViewPublications.Id = ActiveSubscriptions.PublicationId
    group by
        ViewPublications.Id, ViewPublications.PubIndex, ViewPublications.Title,
        ViewPublications.PublicationType
    order by
        ViewPublications.PublicationType, ViewPublications.Title;
end;
go


-- Запрос 2. По заданному адресу - фамилия почтальона, обслуживающего подписчика
create or alter proc PostmanByAddress
    @street    nvarchar(60),    -- улица: «ул. Ленина»
    @building  nvarchar(10)     -- номер дома: «12»
as
begin
    select
        ViewHouses.PostmanSurname
        , ViewHouses.PostmanShortName
        , ViewHouses.DistrictNumber
        , ViewHouses.Street
        , ViewHouses.Building
    from
        ViewHouses
    where
        ViewHouses.Street = @street
        and ViewHouses.Building = @building;
end;
go


-- Запрос 3. Какие газеты выписывает гражданин с указанными фамилией, именем, отчеством;
-- адрес в выдаче различает полных тёзок
create or alter proc CitizenNewspapers
    @surname     nvarchar(50),
    @name        nvarchar(40),
    @patronymic  nvarchar(50),  -- null или пустая строка - отчества нет
    @onDate      date = null    -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        ActiveSubscriptions.PubIndex
        , ActiveSubscriptions.Title
        , ActiveSubscriptions.DateStart
        , ActiveSubscriptions.DateEnd
        , ActiveSubscriptions.Copies
        , ActiveSubscriptions.Address
    from
        dbo.ActiveSubscriptions(@onDate) as ActiveSubscriptions
    where
        ActiveSubscriptions.PublicationType = 'Газета'
        and ActiveSubscriptions.Surname = @surname
        and ActiveSubscriptions.[Name] = @name
        and isnull(ActiveSubscriptions.Patronymic, '') = isnull(@patronymic, '');
end;
go


-- Запрос 4. Сколько почтальонов работает в почтовом отделении
create or alter proc WorkingPostmenCount
as
begin
    select
        count(*) as PostmenCount
    from
        ViewWorkingPostmen;
end;
go


-- Запрос 5. На каком участке количество экземпляров подписных изданий максимально;
-- при равенстве выводятся все участки с максимумом
create or alter proc MaxCopiesDistricts
    @onDate date = null         -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        DistrictsCopies.DistrictNumber
        , DistrictsCopies.PostmanShortName
        , DistrictsCopies.Copies
    from
        dbo.DistrictsCopies(@onDate) as DistrictsCopies
    where
        DistrictsCopies.Copies = (select max(Copies) from dbo.DistrictsCopies(@onDate));
end;
go


-- Запрос 6. Средний срок подписки по каждому изданию за всю историю:
-- «каждому» - обычное соединение, в выдаче только издания с подписками
create or alter proc AverageTermByPublication
as
begin
    select
        ViewSubscriptions.PublicationId
        , ViewSubscriptions.PubIndex
        , ViewSubscriptions.Title
        , avg(cast(ViewSubscriptions.Duration as float)) as AverageDuration
    from
        ViewSubscriptions
    group by
        ViewSubscriptions.PublicationId, ViewSubscriptions.PubIndex, ViewSubscriptions.Title
    order by
        ViewSubscriptions.Title;
end;
go


-- Справка: количество подписчиков, газет и журналов по подпискам, оформленным
-- в заданном периоде; приложение передаёт 1 января и 31 декабря текущего года
create or alter proc SubscriptionsSummary
    @dateFrom  date,            -- начало периода
    @dateTo    date             -- конец периода
as
begin
    select
        count(distinct ViewSubscriptions.SubscriberId) as Subscribers
        , isnull(sum(iif(ViewSubscriptions.PublicationType = 'Газета', ViewSubscriptions.Copies, 0)), 0)
            as Newspapers
        , isnull(sum(iif(ViewSubscriptions.PublicationType = 'Журнал', ViewSubscriptions.Copies, 0)), 0)
            as Magazines
    from
        ViewSubscriptions
    where
        ViewSubscriptions.IssueDate between @dateFrom and @dateTo;
end;
go


-- Отчёт о доставке, строки: по каждому участку почтальон и доставляемые издания;
-- итоги по изданию и участку считает приложение
create or alter proc DeliveryLines
    @onDate date = null         -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        ActiveSubscriptions.DistrictNumber
        , ActiveSubscriptions.PostmanShortName
        , ActiveSubscriptions.PubIndex
        , ActiveSubscriptions.Title
        , ActiveSubscriptions.Address
        , ActiveSubscriptions.DateStart
        , ActiveSubscriptions.DateEnd
        , ActiveSubscriptions.Duration
        , ActiveSubscriptions.Copies
    from
        dbo.ActiveSubscriptions(@onDate) as ActiveSubscriptions
    order by
        ActiveSubscriptions.DistrictNumber, ActiveSubscriptions.Title, ActiveSubscriptions.Address;
end;
go


-- Отчёт о доставке, итоги: сколько почтальонов работает, сколько всего участков,
-- сколько различных изданий доставляется
create or alter proc DeliveryTotals
    @onDate date = null         -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        (select count(*) from ViewWorkingPostmen) as Postmen
        , (select count(*) from Districts) as Districts
        , (select count(distinct PublicationId) from dbo.ActiveSubscriptions(@onDate)) as Publications;
end;
go


-- Кабинет подписчика: все его подписки и их состояние на дату, от новых к старым
create or alter proc SubscriberHistory
    @subscriberId  int,
    @onDate        date = null  -- дата, null - сегодня
as
begin
    set @onDate = isnull(@onDate, getdate());

    select
        SubscriptionsWithStatus.Id
        , SubscriptionsWithStatus.PubIndex
        , SubscriptionsWithStatus.Title
        , SubscriptionsWithStatus.PublicationType
        , SubscriptionsWithStatus.IssueDate
        , SubscriptionsWithStatus.DateStart
        , SubscriptionsWithStatus.DateEnd
        , SubscriptionsWithStatus.Duration
        , SubscriptionsWithStatus.Copies
        , SubscriptionsWithStatus.Price
        , SubscriptionsWithStatus.Cost
        , SubscriptionsWithStatus.[Status]
    from
        dbo.SubscriptionsWithStatus(@onDate) as SubscriptionsWithStatus
    where
        SubscriptionsWithStatus.SubscriberId = @subscriberId
    order by
        SubscriptionsWithStatus.DateStart desc;
end;
go


-- Кабинет почтальона: дома его участков
create or alter proc PostmanHouses
    @postmanId int              -- работник-почтальон
as
begin
    select
        ViewHouses.DistrictNumber
        , ViewHouses.Street
        , ViewHouses.Building
    from
        ViewHouses
    where
        ViewHouses.PostmanId = @postmanId
    order by
        ViewHouses.DistrictNumber, ViewHouses.Street, ViewHouses.Building;
end;
go


-- Участки и их почтальоны - для окон приёма почтальона и передачи участка
create or alter proc DistrictsList
as
begin
    select
        ViewDistricts.Id
        , ViewDistricts.PostmanId
        , ViewDistricts.Number
        , ViewDistricts.PostmanShortName
    from
        ViewDistricts
    order by
        ViewDistricts.Number;
end;
go


-- Работающие почтальоны и число их участков - для окон увольнения и передачи участка
create or alter proc WorkingPostmenList
as
begin
    select
        ViewWorkingPostmen.Id
        , ViewWorkingPostmen.ShortName
        , ViewWorkingPostmen.DistrictCount
    from
        ViewWorkingPostmen
    order by
        ViewWorkingPostmen.ShortName;
end;
go


-- Подписчики с адресами - для окон оформления подписки и выдачи логина
create or alter proc SubscribersList
as
begin
    select
        ViewSubscribers.Id
        , ViewSubscribers.PersonId
        , ViewSubscribers.ShortName
        , ViewSubscribers.Passport
        , ViewSubscribers.Address
        , ViewSubscribers.DistrictNumber
    from
        ViewSubscribers
    order by
        ViewSubscribers.Surname, ViewSubscribers.[Name];
end;
go


-- Дома с адресами - выбор адреса в запросе 2
create or alter proc HousesList
as
begin
    select
        ViewHouses.Id
        , ViewHouses.Street
        , ViewHouses.Building
        , dbo.FullAddress(ViewHouses.Street, ViewHouses.Building, null) as Address
        , ViewHouses.DistrictNumber
    from
        ViewHouses
    order by
        ViewHouses.Street, ViewHouses.Building;
end;
go


-- Граждане-подписчики по ФИО без повторов - выбор гражданина в запросе 3;
-- полные тёзки дают одну строку
create or alter proc CitizensList
as
begin
    select distinct
        ViewSubscribers.Surname
        , ViewSubscribers.[Name]
        , ViewSubscribers.Patronymic
        , ViewSubscribers.Surname + ' ' + ViewSubscribers.[Name]
            + isnull(' ' + ViewSubscribers.Patronymic, '') as FullName
    from
        ViewSubscribers
    order by
        FullName;
end;
go


-- Предварительная стоимость в окне оформления подписки: цена издания по каталогу,
-- срок и экземпляры - по той же формуле SubscriptionCost, что и в квитанции
create or alter proc OrderCost
    @publicationId  int,        -- издание
    @duration       int,        -- срок в месяцах
    @copies         int         -- количество экземпляров
as
begin
    select
        dbo.SubscriptionCost(Publications.Price, @duration, @copies) as Cost
    from
        Publications
    where
        Publications.Id = @publicationId;
end;
go


-- Человек по паспорту - приём почтальона: форма подставляет ФИО того, кто уже есть
-- в базе, сразу отказывает тому, кто уже работает, и не просит логин у того,
-- у кого есть учётная запись. Нет строки - человека с таким паспортом нет
create or alter proc PersonByPassport
    @passport  nvarchar(15)     -- серия и номер паспорта
as
begin
    select
        People.Id
        , People.Surname
        , People.[Name]
        , People.Patronymic
        , (select count(*) from ViewWorkingEmployees
           where ViewWorkingEmployees.PersonId = People.Id) as Working
        , (select count(*) from Accounts
           where Accounts.PersonId = People.Id) as HasAccount
    from
        People
    where
        People.Passport = @passport;
end;
go


-- Данные квитанции для файла
create or alter proc Receipt
    @subscriptionId int         -- номер квитанции
as
begin
    select
        ViewSubscriptions.Id
        , ViewSubscriptions.IssueDate
        , ViewSubscriptions.Surname
        , ViewSubscriptions.[Name]
        , ViewSubscriptions.Patronymic
        , ViewSubscriptions.Address
        , ViewSubscriptions.OperatorShortName
        , ViewSubscriptions.PubIndex
        , ViewSubscriptions.Title
        , ViewSubscriptions.PublicationType
        , ViewSubscriptions.DateStart
        , ViewSubscriptions.DateEnd
        , ViewSubscriptions.Duration
        , ViewSubscriptions.Copies
        , ViewSubscriptions.Price
        , ViewSubscriptions.Cost
    from
        ViewSubscriptions
    where
        ViewSubscriptions.Id = @subscriptionId;
end;
go


/**** Операции ****/

-- Регистрация подписчика: гостем (логин обязателен - проверяет форма) или оператором
-- (логин можно не задавать). Если человек уже есть в базе (например, работник),
-- подписчиком становится он же, по паспорту
create or alter proc RegisterSubscriber
    @passport      nvarchar(15),
    @surname       nvarchar(50),
    @name          nvarchar(40),
    @patronymic    nvarchar(50),            -- null или пустая строка - отчества нет
    @houseId       int,                     -- дом
    @flat          int,                     -- квартира, null - частный дом
    @login         nvarchar(30) = null,     -- null - без учётной записи
    @password      nvarchar(50) = null,
    @subscriberId  int output,              -- новый подписчик
    @message       nvarchar(200) output     -- причина отказа, null - подписчик зарегистрирован
as
begin
    declare @count int, @personId int = null;
    set @message = null;
    set @subscriberId = null;

    if (@patronymic = '') set @patronymic = null;
    if (@login = '') set @login = null;

    -- дом обслуживает отделение
    select
        @count = count(*)
    from
        Houses
    where
        Houses.Id = @houseId;

    if (@count = 0) begin
        set @message = 'Дом не обслуживается отделением';
        return;
    end;

    -- человек с таким паспортом уже есть: ФИО должно совпадать, подписчиком он ещё не стал
    select
        @personId = People.Id
    from
        People
    where
        People.Passport = @passport;

    if (@personId is not null) begin
        select
            @count = count(*)
        from
            People
        where
            People.Id = @personId
            and People.Surname = @surname
            and People.[Name] = @name
            and isnull(People.Patronymic, '') = isnull(@patronymic, '');

        if (@count = 0) begin
            select
                @message = 'Паспорт ' + @passport + ' принадлежит: '
                    + dbo.ShortName(People.Surname, People.[Name], People.Patronymic)
            from
                People
            where
                People.Id = @personId;
            return;
        end;

        select
            @count = count(*)
        from
            Subscribers
        where
            Subscribers.PersonId = @personId;

        if (@count > 0) begin
            set @message = 'Этот человек уже зарегистрирован подписчиком';
            return;
        end;
    end;

    -- логин свободен, пароль задан, учётной записи у человека ещё нет
    if (@login is not null) begin
        if (isnull(@password, '') = '') begin
            set @message = 'Для учётной записи нужен пароль';
            return;
        end;

        select
            @count = count(*)
        from
            Accounts
        where
            Accounts.[Login] = @login;

        if (@count > 0) begin
            set @message = 'Логин ' + @login + ' уже занят';
            return;
        end;

        select
            @count = count(*)
        from
            Accounts
        where
            Accounts.PersonId = @personId;

        if (@count > 0) begin
            set @message = 'У этого человека уже есть учётная запись';
            return;
        end;
    end;

    declare @errPeople int = 0, @errSubscribers int = 0, @errAccounts int = 0;
    begin tran;

    if (@personId is null) begin
        insert into People
            (Surname, [Name], Patronymic, Passport)
        values
            (@surname, @name, @patronymic, @passport);
        set @errPeople = @@error;

        select
            @personId = People.Id
        from
            People
        where
            People.Passport = @passport;
    end;

    insert into Subscribers
        (PersonId, HouseId, Flat)
    values
        (@personId, @houseId, @flat);
    set @errSubscribers = @@error;

    if (@login is not null) begin
        insert into Accounts
            (PersonId, [Login], PasswordHash)
        values
            (@personId, @login, dbo.PasswordHash(@login, @password));
        set @errAccounts = @@error;
    end;

    if (@errPeople = 0 and @errSubscribers = 0 and @errAccounts = 0) begin
        commit tran;

        select
            @subscriberId = Subscribers.Id
        from
            Subscribers
        where
            Subscribers.PersonId = @personId;
    end else begin
        rollback tran;
        set @message = 'Не удалось зарегистрировать подписчика';
    end;
end;
go


-- Учётная запись человеку, у которого её ещё нет (оператор выдаёт логин клиенту)
create or alter proc SetAccount
    @personId  int,
    @login     nvarchar(30),
    @password  nvarchar(50),
    @message   nvarchar(200) output         -- причина отказа, null - учётная запись создана
as
begin
    declare @count int;
    set @message = null;

    if (isnull(@login, '') = '' or isnull(@password, '') = '') begin
        set @message = 'Нужны логин и пароль';
        return;
    end;

    select
        @count = count(*)
    from
        People
    where
        People.Id = @personId;

    if (@count = 0) begin
        set @message = 'Человек не найден';
        return;
    end;

    select
        @count = count(*)
    from
        Accounts
    where
        Accounts.PersonId = @personId;

    if (@count > 0) begin
        set @message = 'У этого человека уже есть учётная запись';
        return;
    end;

    select
        @count = count(*)
    from
        Accounts
    where
        Accounts.[Login] = @login;

    if (@count > 0) begin
        set @message = 'Логин ' + @login + ' уже занят';
        return;
    end;

    insert into Accounts
        (PersonId, [Login], PasswordHash)
    values
        (@personId, @login, dbo.PasswordHash(@login, @password));

    if (@@error <> 0) begin
        set @message = 'Не удалось создать учётную запись';
    end;
end;
go


-- Смена пароля пользователем; пароль, выданный оператором или заведующим, - временный
create or alter proc ChangePassword
    @login        nvarchar(30),
    @oldPassword  nvarchar(50),
    @newPassword  nvarchar(50),
    @message      nvarchar(200) output      -- причина отказа, null - пароль изменён
as
begin
    declare @count int;
    set @message = null;

    -- логин ищется без учёта регистра, хеш считается от сохранённого логина
    select
        @count = count(*)
    from
        Accounts
    where
        Accounts.[Login] = @login
        and Accounts.PasswordHash = dbo.PasswordHash(Accounts.[Login], @oldPassword);

    if (@count = 0) begin
        set @message = 'Неверный логин или пароль';
        return;
    end;

    if (isnull(@newPassword, '') = '') begin
        set @message = 'Задайте новый пароль';
        return;
    end;

    update
        Accounts
    set
        PasswordHash = dbo.PasswordHash(Accounts.[Login], @newPassword)
    where
        Accounts.[Login] = @login;

    if (@@error <> 0) begin
        set @message = 'Не удалось изменить пароль';
    end;
end;
go


-- Вход: роли учётной записи - строки ViewAccountRoles; работник-подписчик получает две строки
create or alter proc CheckLogin
    @login     nvarchar(30),
    @password  nvarchar(50),
    @message   nvarchar(200) output         -- причина отказа, null - вход выполнен
as
begin
    declare @count int;
    set @message = null;

    select
        @count = count(*)
    from
        Accounts
    where
        Accounts.[Login] = @login
        and Accounts.PasswordHash = dbo.PasswordHash(Accounts.[Login], @password);

    if (@count = 0) begin
        set @message = 'Неверный логин или пароль';
        return;
    end;

    select
        @count = count(*)
    from
        ViewAccountRoles
    where
        ViewAccountRoles.[Login] = @login;

    if (@count = 0) begin
        set @message = 'Учётная запись больше не действует: вы не работаете в отделении и не являетесь подписчиком';
        return;
    end;

    select
        ViewAccountRoles.PersonId
        , ViewAccountRoles.EmployeeId
        , ViewAccountRoles.SubscriberId
        , ViewAccountRoles.[Login]
        , ViewAccountRoles.ShortName
        , ViewAccountRoles.[Role]
    from
        ViewAccountRoles
    where
        ViewAccountRoles.[Login] = @login;
end;
go


-- Оформление подписки с выдачей квитанции: цена копируется из каталога,
-- дата оформления - сегодня; квитанция - эта подписка, её номер - @subscriptionId
create or alter proc CreateSubscription
    @subscriberId    int,                   -- подписчик
    @operatorId      int,                   -- работник, оформляющий подписку
    @publicationId   int,                   -- издание
    @dateStart       date,                  -- начало подписки, 1-е число месяца
    @duration        int,                   -- срок в месяцах
    @copies          int,                   -- количество экземпляров
    @subscriptionId  int output,            -- номер квитанции
    @message         nvarchar(200) output   -- причина отказа, null - подписка оформлена
as
begin
    declare @count int, @today date = getdate();
    set @message = null;
    set @subscriptionId = null;

    select
        @count = count(*)
    from
        Subscribers
    where
        Subscribers.Id = @subscriberId;

    if (@count = 0) begin
        set @message = 'Подписчик не найден';
        return;
    end;

    -- оформляет работающий оператор или заведующий
    select
        @count = count(*)
    from
        ViewWorkingEmployees
    where
        ViewWorkingEmployees.Id = @operatorId
        and ViewWorkingEmployees.[Role] in ('Оператор', 'Заведующий');

    if (@count = 0) begin
        set @message = 'Подписку оформляет работающий оператор или заведующий';
        return;
    end;

    select
        @count = count(*)
    from
        Publications
    where
        Publications.Id = @publicationId;

    if (@count = 0) begin
        set @message = 'Издания нет в каталоге';
        return;
    end;

    if (datepart(day, @dateStart) <> 1
        or @dateStart not between dateadd(day, 1, @today) and dateadd(month, 12, @today)) begin
        set @message = 'Подписка начинается 1-го числа, не раньше следующего месяца и не позже чем через 12 месяцев';
        return;
    end;

    if (@duration not between 1 and 12) begin
        set @message = 'Срок подписки - от 1 до 12 месяцев';
        return;
    end;

    if (@copies < 1) begin
        set @message = 'Количество экземпляров - не меньше одного';
        return;
    end;

    -- подписка на это издание у подписчика не должна пересекаться по датам с другой;
    -- выборка без строк оставляет @message пустым
    declare @dateEnd date = dbo.SubscriptionEndDate(@dateStart, @duration);

    select top (1)
        @message = 'Уже есть подписка на ' + ViewSubscriptions.PubIndex
            + ' с ' + convert(nvarchar(10), ViewSubscriptions.DateStart, 104)
            + ' по ' + convert(nvarchar(10), ViewSubscriptions.DateEnd, 104)
    from
        ViewSubscriptions
    where
        ViewSubscriptions.SubscriberId = @subscriberId
        and ViewSubscriptions.PublicationId = @publicationId
        and (@dateStart between ViewSubscriptions.DateStart and ViewSubscriptions.DateEnd
             or ViewSubscriptions.DateStart between @dateStart and @dateEnd);

    if (@message is not null) begin
        return;
    end;

    insert into Subscriptions
        (SubscriberId, OperatorId, PublicationId, DateStart, Duration, Copies, Price)
    select
        @subscriberId, @operatorId, @publicationId, @dateStart, @duration, @copies, Publications.Price
    from
        Publications
    where
        Publications.Id = @publicationId;

    if (@@error <> 0) begin
        set @message = 'Не удалось оформить подписку';
        return;
    end;

    select
        @subscriptionId = Subscriptions.Id
    from
        Subscriptions
    where
        Subscriptions.SubscriberId = @subscriberId
        and Subscriptions.PublicationId = @publicationId
        and Subscriptions.DateStart = @dateStart;
end;
go


-- Приём почтальона с выделением участка; участок переходит к нему от прежнего почтальона.
-- Если человек уже есть в базе (подписчик или уволенный работник) - принимается он же,
-- уволенному почтальону очищается дата увольнения
create or alter proc HirePostman
    @passport    nvarchar(15),
    @surname     nvarchar(50),
    @name        nvarchar(40),
    @patronymic  nvarchar(50),              -- null или пустая строка - отчества нет
    @login       nvarchar(30) = null,       -- обязателен, если учётной записи ещё нет
    @password    nvarchar(50) = null,
    @districtId  int,                       -- участок, который выделяется почтальону
    @employeeId  int output,                -- принятый работник
    @message     nvarchar(200) output       -- причина отказа, null - почтальон принят
as
begin
    declare @count int, @personId int = null, @postmanRoleId int, @hasAccount int = 0,
        @today date = getdate();
    set @message = null;
    set @employeeId = null;

    if (@patronymic = '') set @patronymic = null;
    if (@login = '') set @login = null;

    select
        @postmanRoleId = Roles.Id
    from
        Roles
    where
        Roles.[Name] = 'Почтальон';

    select
        @count = count(*)
    from
        Districts
    where
        Districts.Id = @districtId;

    if (@count = 0) begin
        set @message = 'Участок не найден';
        return;
    end;

    -- человек с таким паспортом уже есть: ФИО совпадает, сейчас он не работает
    select
        @personId = People.Id
    from
        People
    where
        People.Passport = @passport;

    if (@personId is not null) begin
        select
            @count = count(*)
        from
            People
        where
            People.Id = @personId
            and People.Surname = @surname
            and People.[Name] = @name
            and isnull(People.Patronymic, '') = isnull(@patronymic, '');

        if (@count = 0) begin
            select
                @message = 'Паспорт ' + @passport + ' принадлежит: '
                    + dbo.ShortName(People.Surname, People.[Name], People.Patronymic)
            from
                People
            where
                People.Id = @personId;
            return;
        end;

        select
            @count = count(*)
        from
            ViewWorkingEmployees
        where
            ViewWorkingEmployees.PersonId = @personId;

        if (@count > 0) begin
            set @message = 'Этот человек уже работает в отделении';
            return;
        end;

        select
            @hasAccount = count(*)
        from
            Accounts
        where
            Accounts.PersonId = @personId;
    end;

    -- учётная запись: есть - логин не задаётся; нет - нужны свободный логин и пароль
    if (@hasAccount > 0 and @login is not null) begin
        set @message = 'У этого человека уже есть учётная запись, логин не задаётся';
        return;
    end;

    if (@hasAccount = 0) begin
        if (@login is null or isnull(@password, '') = '') begin
            set @message = 'Для учётной записи почтальона нужны логин и пароль';
            return;
        end;

        select
            @count = count(*)
        from
            Accounts
        where
            Accounts.[Login] = @login;

        if (@count > 0) begin
            set @message = 'Логин ' + @login + ' уже занят';
            return;
        end;
    end;

    declare @errPeople int = 0, @errEmployees int = 0, @errAccounts int = 0, @errDistricts int = 0;
    begin tran;

    if (@personId is null) begin
        insert into People
            (Surname, [Name], Patronymic, Passport)
        values
            (@surname, @name, @patronymic, @passport);
        set @errPeople = @@error;

        select
            @personId = People.Id
        from
            People
        where
            People.Passport = @passport;
    end;

    -- уволенный работник - повторный приём той же строкой, иначе новая строка
    select
        @count = count(*)
    from
        Employees
    where
        Employees.PersonId = @personId;

    if (@count > 0) begin
        update
            Employees
        set
            RoleId = @postmanRoleId
            , HireDate = @today
            , FireDate = null
        where
            Employees.PersonId = @personId;
        set @errEmployees = @@error;
    end else begin
        insert into Employees
            (PersonId, RoleId, HireDate)
        values
            (@personId, @postmanRoleId, @today);
        set @errEmployees = @@error;
    end;

    select
        @employeeId = Employees.Id
    from
        Employees
    where
        Employees.PersonId = @personId;

    if (@hasAccount = 0) begin
        insert into Accounts
            (PersonId, [Login], PasswordHash)
        values
            (@personId, @login, dbo.PasswordHash(@login, @password));
        set @errAccounts = @@error;
    end;

    update
        Districts
    set
        PostmanId = @employeeId
    where
        Districts.Id = @districtId;
    set @errDistricts = @@error;

    if (@errPeople = 0 and @errEmployees = 0 and @errAccounts = 0 and @errDistricts = 0) begin
        commit tran;
    end else begin
        rollback tran;
        set @employeeId = null;
        set @message = 'Не удалось принять почтальона';
    end;
end;
go


-- Передача участка работающему почтальону, у которого меньше предельного числа участков
create or alter proc ReassignDistrict
    @districtId  int,                       -- участок
    @postmanId   int,                       -- новый почтальон участка
    @message     nvarchar(200) output       -- причина отказа, null - участок передан
as
begin
    declare @count int, @max int = dbo.MaxDistrictsPerPostman();
    set @message = null;

    select
        @count = count(*)
    from
        Districts
    where
        Districts.Id = @districtId;

    if (@count = 0) begin
        set @message = 'Участок не найден';
        return;
    end;

    select
        @count = count(*)
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.Id = @postmanId;

    if (@count = 0) begin
        set @message = 'Участок можно передать только работающему почтальону';
        return;
    end;

    select
        @count = count(*)
    from
        Districts
    where
        Districts.Id = @districtId
        and Districts.PostmanId = @postmanId;

    if (@count > 0) begin
        set @message = 'Этот участок уже закреплён за этим почтальоном';
        return;
    end;

    select
        @count = ViewWorkingPostmen.DistrictCount
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.Id = @postmanId;

    if (@count >= @max) begin
        set @message = 'У почтальона уже ' + cast(@count as nvarchar(10))
            + ' участков - больше передать нельзя';
        return;
    end;

    update
        Districts
    set
        PostmanId = @postmanId
    where
        Districts.Id = @districtId;

    if (@@error <> 0) begin
        set @message = 'Не удалось передать участок';
    end;
end;
go


-- Увольнение почтальона: в одной транзакции его участки по одному, в порядке номеров,
-- уходят работающим почтальонам с наименьшим числом участков (кроме увольняемого,
-- при равенстве - с меньшим Id), затем ставится дата увольнения.
-- Если свободных мест у остальных не хватает - ничего не меняется
create or alter proc FirePostman
    @postmanId  int,                        -- увольняемый почтальон
    @message    nvarchar(200) output        -- причина отказа, null - почтальон уволен
as
begin
    declare @count int, @free int, @districtId int, @newPostmanId int, @err int = 0,
        @max int = dbo.MaxDistrictsPerPostman(), @today date = getdate();
    set @message = null;

    select
        @count = count(*)
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.Id = @postmanId;

    if (@count = 0) begin
        set @message = 'Уволить можно только работающего почтальона';
        return;
    end;

    -- хватит ли свободных мест у остальных почтальонов на все его участки
    select
        @count = count(*)
    from
        Districts
    where
        Districts.PostmanId = @postmanId;

    select
        @free = isnull(sum(@max - ViewWorkingPostmen.DistrictCount), 0)
    from
        ViewWorkingPostmen
    where
        ViewWorkingPostmen.Id <> @postmanId;

    if (@count > @free) begin
        set @message = 'Нельзя уволить: у почтальона ' + cast(@count as nvarchar(10))
            + ' участков, а свободных мест у остальных ' + cast(@free as nvarchar(10))
            + ' (не больше ' + cast(@max as nvarchar(10))
            + ' на почтальона). Сначала примите нового почтальона';
        return;
    end;

    begin tran;

    while (@count > 0 and @err = 0) begin
        select top (1)
            @districtId = Districts.Id
        from
            Districts
        where
            Districts.PostmanId = @postmanId
        order by
            Districts.Number;

        -- выборка без строк оставляет в переменной старое значение
        set @newPostmanId = null;
        select top (1)
            @newPostmanId = ViewWorkingPostmen.Id
        from
            ViewWorkingPostmen
        where
            ViewWorkingPostmen.Id <> @postmanId
            and ViewWorkingPostmen.DistrictCount < @max
        order by
            ViewWorkingPostmen.DistrictCount, ViewWorkingPostmen.Id;

        update
            Districts
        set
            PostmanId = @newPostmanId
        where
            Districts.Id = @districtId;
        set @err = @err + @@error;

        select
            @count = count(*)
        from
            Districts
        where
            Districts.PostmanId = @postmanId;
    end;

    update
        Employees
    set
        FireDate = @today
    where
        Employees.Id = @postmanId;
    set @err = @err + @@error;

    if (@err = 0) begin
        commit tran;
    end else begin
        rollback tran;
        set @message = 'Не удалось уволить почтальона';
    end;
end;
go

print '*** Завершено создание хранимых процедур ***';
go
