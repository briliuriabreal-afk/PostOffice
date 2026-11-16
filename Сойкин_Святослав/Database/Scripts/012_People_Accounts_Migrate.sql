/**** Перенос существующих учётных записей в People без удаления людей и подписок ****/
-- Для базы прежней версии. После него выполнить 002, 003 и 006, не выполняя 005.
-- Входы сохраняются. Учебным людям без входа выдаются логины из заполнения, пароль post2026.
-- Если есть другие люди без входа, перенос прервётся: им сначала нужны свои учётные данные.
use PostOffice_Soykin_PE521;
go
set xact_abort on;
begin tran;
if object_id('Accounts') is null
    throw 51000, 'База уже обновлена: перенос Accounts не требуется', 1;
if col_length('People', 'Login') is null
    alter table People add [Login] nvarchar(30) null, PasswordHash varbinary(32) null;
go
update
    People
set
    [Login] = Accounts.[Login]
    , PasswordHash = Accounts.PasswordHash
from
    People join Accounts on People.Id = Accounts.PersonId;

update
    People
set
    [Login] = Seed.[Login]
    , PasswordHash = Seed.PasswordHash
from
    People join (values
    ('4501 112233', 'belova', 0x1DF653D96C6AB5CACE135EB1A1CA3268BE2382C640585B5ACDA40894086847F7),
    ('4502 223344', 'kozlova', 0x658E1E6F804D309300281760A0866E4794A63B3B7D54E361B6D936556EE69C09),
    ('4503 334455', 'morozov', 0x5FEBE5105DCD81167414F707BFAD45FBDABA301EDC99CCF8EA25553B958CF415),
    ('4504 445566', 'petrov', 0x88CBAC2CFE964A6B5F990860394BB88C05ACDC3579713899DAE35A70B0462BAF),
    ('4505 556677', 'smirnova', 0x325621F6120A012012352835EB7B2196582D2C81B426BB21132A7C67C6FC1118),
    ('4506 667788', 'vasiliev', 0xAE8C5D3AB5281576365592442CAA8E4E24DBB18F11FC4D0EB292B46D1E232C63),
    ('4507 778899', 'nikolaev', 0x0EE025B226356E53236BAF3D3B15B3C364281FCEEC32C29F8CE8EFED7C59462E),
    ('4508 889900', 'fedorova', 0x2E559CDC36824EE7C4DA1948D56022499DA9191FC45AB20612E02C507BBE5E4E),
    ('4510 100201', 'kuznetsov', 0x904B44D45EF5C0C2577D1751599A9711D38D120AB551BF82C2947C184F24CF29),
    ('4510 100202', 'li', 0x200B49D44E3A8BB982965BF076652820597E62EA240259F127AB66E7542F33BC),
    ('4510 100203', 'ivanov', 0x1B55F790033ED3C3AE7EEA5220023BC488E6AC62E6CB52417A79C8F002D02403),
    ('4510 100204', 'ivanov2', 0x0E820D9D942279169086440C1D2EB6B7BF0CFC60E20512AB40C85C0C95E551F0),
    ('4510 100205', 'sokolova', 0x48B0F0F7E71167F83E16958CD99A19C6D3680F0FA6EF2043CC44B1C20568279F),
    ('4510 100206', 'sokolov', 0xAA1E60270BE671468D8DD7ECABFDAAAF5BE84F66EAD1A4D30519B4660BCAEC29),
    ('4510 100207', 'orlova', 0x7C47E2146116ECDC55F624DAA0B3ADB4399E374263B589B581DCFE1D5E40783E),
    ('4510 100208', 'lebedev', 0x0D1BA30D3FF42D453B32FE356240FB70CB6DAEEE8462AAFD6596C60F673E7ADB),
    ('4510 100209', 'novikova', 0xE75B152E1443F68BE8F96F966521A277020040DBDAC2A9C604D6C9EA10A18B30),
    ('4510 100210', 'popov', 0x1E694368C0DC7A612CE58F8822F8D05DD3553E79DD06967906DECB2069FEA103),
    ('4510 100211', 'volkova', 0xE2013238853408AD295B09EFB96F1CD05B47305292EF53B391E67852126EDB16),
    ('4510 100212', 'zaytsev', 0xA44F3057A4E99E0516E69FFFA9A42DA73B249794E5E3C7D3B7DFC70DF272CA33),
    ('4510 100213', 'pavlova', 0x24525766C9A1C4E1D091AE7B2F28480F2A465D7A84FC1DD5AACF01887E4EC43C),
    ('4510 100214', 'semenov', 0x29FFF5E1006E311B6489DCA5E76655A392351AA8DF9882EE3E236ECB41F3A210),
    ('4510 100215', 'golubeva', 0x6BDDF812CD22726C54ABC971CE44E16FDFB9C5E24485007D5060449B2BB0F9C7),
    ('4510 100216', 'vinogradov', 0x1A4A3DA0748C35A90E098FD62F2F2C802CDE2754F8C7F275ECB9426EC463B441),
    ('4510 100217', 'bogdanova', 0x5DD0FB8953F7078EEC822E7019767811CC3DCCEE6FC419DCEA5CB52EF5A0FCBE),
    ('4510 100218', 'tarasov', 0x76D59C381F7F370A1AE34270A97657C99AEF62FBE86E4F65B2840D16E3B7D90C),
    ('4510 100219', 'ershova', 0xC141432DE2E9C291DC11715DE1AC97F2CF31C07A5E924B807F1027FB1DAE79E7)
    ) Seed(Passport, [Login], PasswordHash) on People.Passport = Seed.Passport
where
    People.[Login] is null;

if exists (select * from People where [Login] is null or PasswordHash is null)
    throw 51001, 'Есть люди без входа, не входящие в учебное заполнение. Перенос отменён', 1;

alter table People alter column [Login] nvarchar(30) not null;
alter table People alter column PasswordHash varbinary(32) not null;
alter table People add constraint UQ_People_Login unique ([Login]);
alter table People add constraint CK_People_Login check ([Login] <> '');
alter table People add constraint CK_People_PasswordHash check (datalength(PasswordHash) = 32);
drop table Accounts;
drop procedure if exists SetAccount;
drop function if exists PasswordHash;
commit tran;
go
