INSERT INTO [dbo].[LCCustomers]
           ([CompanyName]
           ,[Address]
           ,[DateCreated]
           ,[City]
           ,[StateProvince]
           ,[Country]
           ,[Commnet]
           ,[ZipCode])
     VALUES (@CompanyName,
	         @Address,
			 @DateCreated,
			 @City,
			 @StateProvince,
			 @Country,
			 @Comment,
			 @ZipCode)


