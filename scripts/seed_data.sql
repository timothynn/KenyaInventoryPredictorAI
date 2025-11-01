
# scripts/seed-data.sql
-- Seed data script for local development

USE InventoryPredictor;
GO

-- Insert sample locations
INSERT INTO Locations (Id, LocationCode, LocationName, Address, County, Latitude, Longitude, ContactPhone, OperatingHours, IsActive)
VALUES 
    (NEWID(), 'NAIROBI_WH', 'Nairobi Central Warehouse', 'Industrial Area, Nairobi', 'Nairobi', -1.3032, 36.8433, '+254712345678', 'Mon-Sat 7AM-6PM', 1),
    (NEWID(), 'MOMBASA_ST', 'Mombasa Retail Store', 'Moi Avenue, Mombasa', 'Mombasa', -4.0435, 39.6682, '+254723456789', 'Mon-Sun 8AM-8PM', 1),
    (NEWID(), 'KISUMU_BR', 'Kisumu Branch', 'Oginga Odinga Street, Kisumu', 'Kisumu', -0.0917, 34.7680, '+254734567890', 'Mon-Sat 8AM-7PM', 1);
GO

-- Insert sample suppliers
INSERT INTO Suppliers (Id, SupplierCode, SupplierName, ContactPerson, ContactPhone, ContactEmail, Address, PaymentTerms, DefaultLeadTimeDays, IsActive, CreatedAt)
VALUES 
    (NEWID(), 'SUPP001', 'Unga Limited', 'John Kamau', '+254700111222', 'sales@unga.co.ke', 'Nairobi, Kenya', 'Net 30', 3, 1, GETUTCDATE()),
    (NEWID(), 'SUPP002', 'Bidco Africa', 'Mary Wanjiku', '+254700222333', 'orders@bidco.com', 'Thika, Kenya', 'Net 30', 5, 1, GETUTCDATE()),
    (NEWID(), 'SUPP003', 'Brookside Dairy', 'Peter Omondi', '+254700333444', 'sales@brookside.co.ke', 'Ruiru, Kenya', 'Net 7', 1, 1, GETUTCDATE());
GO

-- Insert sample users
INSERT INTO Users (Id, Email, PasswordHash, FirstName, LastName, PhoneNumber, Role, IsActive, CreatedAt)
VALUES 
    (NEWID(), 'admin@inventory.co.ke', 'HASHED_PASSWORD_HERE', 'Admin', 'User', '+254700000001', 'Admin', 1, GETUTCDATE()),
    (NEWID(), 'manager@inventory.co.ke', 'HASHED_PASSWORD_HERE', 'Manager', 'User', '+254700000002', 'Manager', 1, GETUTCDATE()),
    (NEWID(), 'user@inventory.co.ke', 'HASHED_PASSWORD_HERE', 'Regular', 'User', '+254700000003', 'User', 1, GETUTCDATE());
GO

PRINT 'Sample data seeded successfully!'
GO