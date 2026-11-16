/**** Почтовое отделение: проверочные вызовы процедур ****/
/*
 * Запускать после заполнения (005). Каждая процедура с параметрами вызывается
 * с тремя наборами из данных заполнения, ожидаемые ответы - в комментариях.
 * Даты наборов: 15.10.2026 - «сейчас», 15.03.2026 - прошлое, 15.03.2027 - будущее.
 * Блок операций в конце меняет данные: после него перезапустить 005 и 008.
 */
use PostOffice_Soykin_PE521;
go


/**** Запрос 1. Экземпляры всех изданий ****/
-- 15.10.2026: Комсомольская правда 6, АиФ 3, Российская газета 2, Вечерний город 3,
--             Спорт-Экспресс 6, Труд 3, Сельская новь 0, Наука и жизнь 1, Вокруг света 1,
--             Огонёк 1, Здоровье 3, Мурзилка 0
exec PublicationsCopies '2026-10-15';
-- 15.03.2026: КП 3, АиФ 2, РГ 1, ВГ 2, СЭ 1, Труд 3, СН 0, НиЖ 1, ВС 1, Огонёк 1, Здоровье 2, Мурзилка 0
exec PublicationsCopies '2026-03-15';
-- 15.03.2027: КП 2, АиФ 2, РГ 2, ВГ 2, СЭ 5, Труд 1, СН 0, НиЖ 1, ВС 2, Огонёк 1, Здоровье 2, Мурзилка 0
exec PublicationsCopies '2027-03-15';
go


/**** Запрос 2. Почтальон по адресу ****/
-- Петров, участок 2
exec PostmanByAddress 'ул. Ленина', '12';
-- Смирнова, участок 6
exec PostmanByAddress 'ул. Садовая', '9';
-- Николаев, участок 10, частный дом
exec PostmanByAddress 'пер. Почтовый', '2';
-- адрес вне территории отделения - пустая выборка
exec PostmanByAddress 'ул. Лесная', '1';
go


/**** Запрос 3. Газеты гражданина ****/
-- только газета Комсомольская правда: журнал Наука и жизнь и завершённая подписка на Труд не выводятся
exec CitizenNewspapers 'Кузнецов', 'Андрей', 'Павлович', '2026-10-15';
-- без отчества: Аргументы и факты
exec CitizenNewspapers 'Ли', 'Виктор', null, '2026-10-15';
-- почтальон-подписчик: Спорт-Экспресс, 3 экземпляра
exec CitizenNewspapers 'Николаев', 'Алексей', 'Игоревич', '2026-10-15';
-- полные тёзки различаются адресом: Российская газета (ул. Ленина) и КП (ул. Садовая)
exec CitizenNewspapers 'Иванов', 'Иван', 'Иванович', '2026-10-15';
go


/**** Запрос 4. Сколько почтальонов работает ****/
-- 4: уволенная Фёдорова, операторы и заведующий не считаются
exec WorkingPostmenCount;
go


/**** Запрос 5. Участок с максимумом экземпляров ****/
-- ничья: участки 3 (Петров) и 10 (Николаев), по 6 экземпляров
exec MaxCopiesDistricts '2026-10-15';
-- участок 6 (Смирнова), 4 экземпляра
exec MaxCopiesDistricts '2026-03-15';
-- участок 8 (Васильев), 4 экземпляра
exec MaxCopiesDistricts '2027-03-15';
go


/**** Запрос 6. Средний срок по каждому изданию ****/
-- КП 6.857, АиФ 7.5, РГ 10, ВГ 10, СЭ 9, Труд 8, НиЖ 8, Вокруг света 7, Огонёк 4.5,
-- Здоровье 9, Мурзилка 4.5; Сельской нови нет - её никто не выписывал
exec AverageTermByPublication;
go


/**** Справка ****/
-- 2026: подписчиков 18, газет 19, журналов 9
exec SubscriptionsSummary '2026-01-01', '2026-12-31';
-- 2025: подписчиков 11, газет 10, журналов 4
exec SubscriptionsSummary '2025-01-01', '2025-12-31';
-- 2024: подписчиков 1, газет 0, журналов 1
exec SubscriptionsSummary '2024-01-01', '2024-12-31';
go


/**** Отчёт о доставке ****/
-- 24 строки, 29 экземпляров; участка 5 нет - там нет подписчиков
exec DeliveryLines '2026-10-15';
-- почтальонов 4, участков 10, изданий 10
exec DeliveryTotals '2026-10-15';
-- 15 строк, 17 экземпляров
exec DeliveryLines '2026-03-15';
exec DeliveryTotals '2026-03-15';
-- 17 строк, 20 экземпляров
exec DeliveryLines '2027-03-15';
exec DeliveryTotals '2027-03-15';
go


/**** Кабинет подписчика ****/
-- Кузнецов: КП и Наука и жизнь действуют, Труд завершена
exec SubscriberHistory 1, '2026-10-15';
-- Николаев: Спорт-Экспресс действует
exec SubscriberHistory 20, '2026-10-15';
-- Иванов (ул. Садовая): Вокруг света ожидает начала, КП действует
exec SubscriberHistory 4, '2026-10-15';
go


/**** Кабинет почтальона ****/
-- Петров: 13 домов участков 1-5
exec PostmanHouses 4;
-- Смирнова: 5 домов участков 6, 7
exec PostmanHouses 5;
-- Николаев: 3 дома участка 10
exec PostmanHouses 7;
go


/**** Списки для окон приложения ****/
-- 10 участков с почтальонами
exec DistrictsList;
-- 4 почтальона: Васильев 2, Николаев 1, Петров 5, Смирнова 2
exec WorkingPostmenList;
-- 21 подписчик с адресами
exec SubscribersList;
-- 26 домов с адресами
exec HousesList;
-- 20 граждан: двое полных тёзок Ивановых дают одну строку
exec CitizensList;

-- предварительная стоимость: КП (120) на 6 месяцев 1 экземпляр - 720,
-- Спорт-Экспресс (110) на 12 месяцев 2 экземпляра - 2640, Вокруг света (450) на 3 месяца 1 экземпляр - 1350
exec OrderCost 1, 6, 1;
exec OrderCost 5, 12, 2;
exec OrderCost 9, 3, 1;

-- человек по паспорту: Козлова работает, учётная запись есть - Working 1, HasAccount 1;
-- Кузнецов - подписчик с учётной записью: 0, 1; Богданова - подписчица без неё: 0, 0;
-- паспорта 4510 999999 нет - пустая выборка
exec PersonByPassport '4502 223344';
exec PersonByPassport '4510 100201';
exec PersonByPassport '4510 100217';
exec PersonByPassport '4510 999999';
go


/**** Квитанция ****/
-- КП по старой цене 110, стоимость 1320 (в каталоге уже 120)
exec Receipt 8;
-- Спорт-Экспресс, 2 экземпляра на 12 месяцев, стоимость 2640
exec Receipt 26;
-- Вокруг света с 01.01.2027 на 6 месяцев, стоимость 2700
exec Receipt 9;
go


/**** Вход ****/
declare @message nvarchar(200);

-- почтальон и подписчик: две строки
exec CheckLogin 'nikolaev', 'post2026', @message output;
print 'nikolaev: ' + isnull(@message, 'вход выполнен');

-- уволенный почтальон: только роль подписчика
exec CheckLogin 'fedorova', 'post2026', @message output;
print 'fedorova: ' + isnull(@message, 'вход выполнен');

-- неверный пароль
exec CheckLogin 'petrov', 'wrong', @message output;
print 'petrov: ' + isnull(@message, 'вход выполнен');
go


/**** Операции - меняют данные, после них перезапустить 005 и 008 ****/

-- Оформление подписки
declare @id int, @message nvarchar(200),
    @nextMonth date = dateadd(month, 1, dateadd(day, 1 - datepart(day, getdate()), getdate()));

-- Орлова выписывает КП со следующего месяца: квитанция оформлена
exec CreateSubscription 7, 2, 1, @nextMonth, 3, 1, @id output, @message output;
print '1. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));

-- то же ещё раз: «Уже есть подписка на 50050 с ... по ...»
exec CreateSubscription 7, 2, 1, @nextMonth, 3, 1, @id output, @message output;
print '2. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));

-- с 15-го числа, задним числом, через 13 месяцев: отказ
declare @day15 date = dateadd(day, 14, @nextMonth), @year date = dateadd(month, 13, @nextMonth);
exec CreateSubscription 7, 2, 2, @day15, 3, 1, @id output, @message output;
print '3. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));
exec CreateSubscription 7, 2, 2, '2026-01-01', 3, 1, @id output, @message output;
print '4. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));
exec CreateSubscription 7, 2, 2, @year, 3, 1, @id output, @message output;
print '5. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));

-- срок 13 месяцев, 0 экземпляров, оформляет почтальон: отказ
exec CreateSubscription 7, 2, 2, @nextMonth, 13, 1, @id output, @message output;
print '6. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));
exec CreateSubscription 7, 2, 2, @nextMonth, 3, 0, @id output, @message output;
print '7. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));
exec CreateSubscription 7, 4, 2, @nextMonth, 3, 1, @id output, @message output;
print '8. ' + isnull(@message, 'оформлена квитанция № ' + cast(@id as nvarchar(10)));
go


-- Регистрация подписчиков и учётные записи
declare @id int, @message nvarchar(200);

-- новый человек с логином: зарегистрирован
exec RegisterSubscriber '4510 200001', 'Егорова', 'Полина', 'Викторовна', 3, 8,
    'egorova', 'post2026', @id output, @message output;
print '9. ' + isnull(@message, 'подписчик № ' + cast(@id as nvarchar(10)));

-- тот же паспорт ещё раз: уже подписчик
exec RegisterSubscriber '4510 200001', 'Егорова', 'Полина', 'Викторовна', 3, 8,
    null, null, @id output, @message output;
print '10. ' + isnull(@message, 'подписчик № ' + cast(@id as nvarchar(10)));

-- паспорт Кузнецова с другим ФИО: отказ
exec RegisterSubscriber '4510 100201', 'Петрова', 'Анна', null, 3, 8,
    null, null, @id output, @message output;
print '11. ' + isnull(@message, 'подписчик № ' + cast(@id as nvarchar(10)));

-- заведующий становится подписчиком: тот же человек, логин не нужен
exec RegisterSubscriber '4501 112233', 'Белова', 'Ирина', 'Сергеевна', 14, 2,
    null, null, @id output, @message output;
print '12. ' + isnull(@message, 'подписчик № ' + cast(@id as nvarchar(10)));

-- занятый логин: отказ
exec RegisterSubscriber '4510 200002', 'Жуков', 'Роман', 'Ильич', 3, 9,
    'petrov', 'post2026', @id output, @message output;
print '13. ' + isnull(@message, 'подписчик № ' + cast(@id as nvarchar(10)));

-- оператор выдаёт логин клиенту без учётной записи; повторно - отказ
exec SetAccount 27, 'ershova', 'post2026', @message output;
print '14. ' + isnull(@message, 'учётная запись создана');
exec SetAccount 27, 'ershova2', 'post2026', @message output;
print '15. ' + isnull(@message, 'учётная запись создана');

-- смена пароля; со старым паролем вход больше не проходит
exec ChangePassword 'li', 'post2026', 'li2026', @message output;
print '16. ' + isnull(@message, 'пароль изменён');
exec CheckLogin 'li', 'post2026', @message output;
print '17. ' + isnull(@message, 'вход выполнен');
go


-- Увольнение: участки уходят наименее загруженным, пока есть место
declare @message nvarchar(200);

-- Николаев: участок 10 - Смирновой (у неё и Васильева по 2, у Смирновой меньший Id)
exec FirePostman 7, @message output;
print '18. ' + isnull(@message, 'Николаев уволен');

-- Васильев: участки 8 и 9 - Смирновой, у Петрова уже 5
exec FirePostman 6, @message output;
print '19. ' + isnull(@message, 'Васильев уволен');

-- Смирнова: у неё 5 участков, у Петрова 5 - места нет, отказ
exec FirePostman 5, @message output;
print '20. ' + isnull(@message, 'Смирнова уволена');

-- уволить оператора нельзя
exec FirePostman 2, @message output;
print '21. ' + isnull(@message, 'Козлова уволена');

-- уволенный Николаев входит только как подписчик
exec CheckLogin 'nikolaev', 'post2026', @message output;
print '22. ' + isnull(@message, 'вход выполнен');
go


-- Приём почтальонов и передача участков
declare @id int, @message nvarchar(200);

-- новый человек с логином, участок 10 переходит от Смирновой
exec HirePostman '4510 200003', 'Карпов', 'Денис', 'Олегович', 'karpov', 'post2026', 10,
    @id output, @message output;
print '23. ' + isnull(@message, 'принят работник № ' + cast(@id as nvarchar(10)));

-- подписчица с учётной записью: логин не задаётся, участок 9
exec HirePostman '4510 100219', 'Ершова', 'Алла', 'Геннадьевна', null, null, 9,
    @id output, @message output;
print '24. ' + isnull(@message, 'принят работник № ' + cast(@id as nvarchar(10)));

-- повторный приём уволенной Фёдоровой той же строкой, участок 6
exec HirePostman '4508 889900', 'Фёдорова', 'Марина', 'Юрьевна', null, null, 6,
    @id output, @message output;
print '25. ' + isnull(@message, 'принят работник № ' + cast(@id as nvarchar(10)));

-- уже работающий Петров: отказ
exec HirePostman '4504 445566', 'Петров', 'Иван', 'Николаевич', null, null, 7,
    @id output, @message output;
print '26. ' + isnull(@message, 'принят работник № ' + cast(@id as nvarchar(10)));

-- участок 1 уже у Петрова; шестой участок Петрову; участок 7 - Карпову
exec ReassignDistrict 1, 4, @message output;
print '27. ' + isnull(@message, 'участок передан');
exec ReassignDistrict 7, 4, @message output;
print '28. ' + isnull(@message, 'участок передан');

select
    @id = ViewWorkingEmployees.Id
from
    ViewWorkingEmployees
where
    ViewWorkingEmployees.Passport = '4510 200003';

exec ReassignDistrict 7, @id, @message output;
print '29. ' + isnull(@message, 'участок передан');

-- итог: работающие почтальоны и их участки
select
    ViewWorkingPostmen.Id
    , ViewWorkingPostmen.ShortName
    , ViewWorkingPostmen.DistrictCount
from
    ViewWorkingPostmen;
go
