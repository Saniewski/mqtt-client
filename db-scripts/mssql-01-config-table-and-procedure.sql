
create table dbo.config (
	IdConfig int primary key not null,
	Code nvarchar(31) not null,
	Value nvarchar(max) null
);
go

insert into dbo.config
(IdConfig, Code, Value) select
1,
'mqtt.client.config',
'{"PushDataInterval":1000,"URI":"127.0.0.1","Port":1883,"Username":null,"Password":null,"Secure":0,"Filters":["mqtt/topics/#"],"Topics":["mqtt/topics/topic1","mqtt/topics/topic2","mqtt/topics/topic3","mqtt/topics/topic4"]}';
go


create or alter procedure dbo.sp_select_config_by_code
	@Code nvarchar(31)
as
begin

	select
		Value as configJson
	from dbo.config
	where Code = @Code;

end
go
