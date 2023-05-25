SELECT * FROM License a
JOIN Customers b on a.CustomersId = b.id
WHERE a.id = @Id


--SELECT * FROM Customers a
--JOIN License b ON a.Id = b.CustomersId
--WHERE a.Id = @id