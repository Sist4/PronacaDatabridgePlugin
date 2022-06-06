USE DBVehiculos
GO
DECLARE @basculaEntrada NVARCHAR(10)='0'
DECLARE @basculaSalida NVARCHAR(10)='0'
DECLARE @placa	NVARCHAR(10)='ABC1234'
DECLARE @chofer NVARCHAR(20)='1'
DECLARE @pesoIngreso NVARCHAR(10)='1500'
DECLARE @pesoSalida NVARCHAR(10)=''
DECLARE @ticket NVARCHAR(100)='1'
DECLARE @pinEntrada NVARCHAR(10)='1234'
DECLARE @pinSalida NVARCHAR(10)=''
DECLARE @operadorEntrada NVARCHAR(20)='Diego'
DECLARE @operadorSalida NVARCHAR(20)=''
DECLARE @rutaImgIng NVARCHAR(50)=''
DECLARE @rutaImgSal NVARCHAR(50)=''
DECLARE @pesosObtenidos NVARCHAR(100)='1500;'
DECLARE @estado VARCHAR(2)='IC'
DECLARE @val1 NVARCHAR(MAX)=''
DECLARE @val2 NVARCHAR(MAX)=''
DECLARE @val3 NVARCHAR(MAX)=''

EXEC P_TBVehiculos @basculaEntrada,@basculaSalida,@placa,@chofer,@pesoIngreso,@pesoSalida,
@ticket,@pinEntrada,@pinSalida,@operadorEntrada,@operadorSalida,@rutaImgIng,@rutaImgSal,
@pesosObtenidos,@estado,@val1,@val2,@val3