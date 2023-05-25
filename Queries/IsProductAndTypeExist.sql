select a.ProductName, b.TypeName from TabProductList a 
join licenseType b on a.[Id] = b.[TabProductListId]
where a.Id = @productid and b.Enum = @enumid
