-- Update existing products with image URLs
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1560806887-1e4cd0b6cbd6?w=400' WHERE Name = 'Organic Honeycrisp Apples';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1571771894821-ce9b6c11b08e?w=400' WHERE Name = 'Fresh Bananas';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1563636619-e9143da7973b?w=400' WHERE Name = 'Whole Milk Gallon';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1509440159596-0249088772ff?w=400' WHERE Name = 'Artisan Sourdough Bread';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1604503468506-a8da13d82791?w=400' WHERE Name = 'Free-Range Chicken Breast';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1499125562588-29fb8a56b5d5?w=400' WHERE Name = 'Atlantic Salmon Fillet';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1576045057995-568f588f82fb?w=400' WHERE Name = 'Organic Baby Spinach';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1546470427-227c7369a9b8?w=400' WHERE Name = 'Roma Tomatoes';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1618164436241-4473940d1f5c?w=400' WHERE Name = 'Sharp Cheddar Cheese';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1488477181946-6428a0291777?w=400' WHERE Name = 'Greek Yogurt';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1555507036-ab1f4038808a?w=400' WHERE Name = 'Chocolate Croissants';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1588168333986-5078d3ae3976?w=400' WHERE Name = 'Ground Beef 80/20';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1464965911861-746a04b4bca6?w=400' WHERE Name = 'Fresh Strawberries';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1523049673857-eb18f1d7b578?w=400' WHERE Name = 'Avocados';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1551462147-ff29053bfc14?w=400' WHERE Name = 'Pasta Penne';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1474979266404-7eaacbcd87c5?w=400' WHERE Name = 'Extra Virgin Olive Oil';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1582722872445-44dc5f7e3c8f?w=400' WHERE Name = 'Organic Eggs';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1570197788417-0e82375c9371?w=400' WHERE Name = 'Vanilla Ice Cream';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1498557850523-fd3d118b962e?w=400' WHERE Name = 'Frozen Blueberries';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1586201375761-83865001e31c?w=400' WHERE Name = 'Quinoa';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1612187209234-a0e7e5c0e1c8?w=400' WHERE Name = 'Almond Butter';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1621506289937-a8e4df240d0b?w=400' WHERE Name = 'Orange Juice';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1556881286-fc6915169721?w=400' WHERE Name = 'Green Tea';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1548907040-4baa42d10919?w=400' WHERE Name = 'Dark Chocolate Bar';
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1536816579748-4ecb3f03d72a?w=400' WHERE Name = 'Mixed Nuts';

-- For any products without images, set a generic placeholder
UPDATE Products SET ImageUrl = 'https://images.unsplash.com/photo-1542838132-92c53300491e?w=400' WHERE ImageUrl IS NULL;
