-- Seed subscription plans
INSERT INTO SubscriptionPlans (Name, Slug, Description, Price, Interval, Features, IsActive, CreatedAt, UpdatedAt) VALUES
('Basic', 'basic', 'Basic plan', 9.99, 'monthly', '["Basic features"]', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
('Pro', 'pro', 'Pro plan', 29.99, 'monthly', '["Advanced features", "API access"]', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());
