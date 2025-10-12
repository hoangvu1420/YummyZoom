--
-- PostgreSQL database dump
--

-- Dumped from database version 16.4 (Debian 16.4-1.pgdg110+2)
-- Dumped by pg_dump version 16.4 (Debian 16.4-1.pgdg110+2)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Data for Name: AccountTransactions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AccountTransactions" ("Id", "RestaurantAccountId", "Type", "Amount", "Currency", "Timestamp", "RelatedOrderId", "Notes", "Created", "CreatedBy") FROM stdin;
\.


--
-- Data for Name: AdminDailyPerformanceSeries; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AdminDailyPerformanceSeries" ("BucketDate", "TotalOrders", "DeliveredOrders", "GrossMerchandiseVolume", "TotalRefunds", "NewCustomers", "NewRestaurants", "UpdatedAtUtc") FROM stdin;
2025-09-12	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-13	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-14	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-15	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-16	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-17	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-18	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-19	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-20	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-21	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-22	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-23	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-24	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-25	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-26	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-27	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-28	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-29	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-09-30	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-01	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-02	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-03	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-04	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-05	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-06	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-07	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-08	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-09	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-10	0	0	0.00	0.00	0	0	2025-10-11 11:13:18.548187+00
2025-10-11	0	0	0.00	0.00	2	3	2025-10-11 11:13:18.548187+00
\.


--
-- Data for Name: AdminPlatformMetricsSnapshots; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AdminPlatformMetricsSnapshots" ("SnapshotId", "TotalOrders", "ActiveOrders", "DeliveredOrders", "GrossMerchandiseVolume", "TotalRefunds", "ActiveRestaurants", "ActiveCustomers", "OpenSupportTickets", "TotalReviews", "LastOrderAtUtc", "UpdatedAtUtc") FROM stdin;
platform	0	0	0	0.00	0.00	3	2	0	0	\N	2025-10-11 11:13:18.548187+00
\.


--
-- Data for Name: AdminRestaurantHealthSummaries; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AdminRestaurantHealthSummaries" ("RestaurantId", "RestaurantName", "IsVerified", "IsAcceptingOrders", "OrdersLast7Days", "OrdersLast30Days", "RevenueLast30Days", "AverageRating", "TotalReviews", "CouponRedemptionsLast30Days", "OutstandingBalance", "LastOrderAtUtc", "UpdatedAtUtc") FROM stdin;
7019b300-9f2a-4758-9745-dd71fb74c327	El Camino Taqueria	t	t	0	0	0.00	0	0	0	0.00	\N	2025-10-11 11:13:18.548187+00
ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Bella Vista Italian	t	t	0	0	0.00	0	0	0	0.00	\N	2025-10-11 11:13:18.548187+00
5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Sakura Sushi	t	t	0	0	0.00	0	0	0	0.00	\N	2025-10-11 11:13:18.548187+00
\.


--
-- Data for Name: AspNetRoles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetRoles" ("Id", "Name", "NormalizedName", "ConcurrencyStamp") FROM stdin;
0199d2f9-6f1c-7ac6-bafa-7eaee262e352	Administrator	ADMINISTRATOR	\N
0199d2f9-6f7f-7573-b37e-d33f845f281c	User	USER	\N
0199d2f9-6f8c-7709-b85a-185303db9033	RestaurantOwner	RESTAURANTOWNER	\N
0199d2f9-6f92-7c9a-913a-d96702f9f5bc	RestaurantStaff	RESTAURANTSTAFF	\N
0199d2f9-6f9a-7531-963e-5fa1c468e8f4	TeamCartHost	TEAMCARTHOST	\N
0199d2f9-6fa0-7a53-878b-92431e302e1e	TeamCartMember	TEAMCARTMEMBER	\N
0199d2f9-6faa-76af-b59e-22cff74e9987	OrderOwner	ORDEROWNER	\N
0199d2f9-6fb4-78da-b080-d5aae6480504	OrderManager	ORDERMANAGER	\N
0199d2f9-6fc0-73bf-909b-d16ecb767cd9	UserOwner	USEROWNER	\N
\.


--
-- Data for Name: AspNetRoleClaims; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetRoleClaims" ("Id", "RoleId", "ClaimType", "ClaimValue") FROM stdin;
\.


--
-- Data for Name: AspNetUsers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount") FROM stdin;
0199d2f9-704a-72c8-ba9d-cfeec6968948	administrator@localhost	ADMINISTRATOR@LOCALHOST	administrator@localhost	ADMINISTRATOR@LOCALHOST	f	AQAAAAIAAYagAAAAEOSwp8ir4R1MKemvKJ5M9/rfCCIH4TGqxr9otvNFDa+Guy4/ke07Jwp9doPky6j74w==	JUQ6JL4YFHEYOPVXRHB3WKOLF73L6JYJ	8e55f048-29d9-45c0-beee-51f987b15c07	\N	f	f	\N	t	0
0199d2f9-7123-7cbc-9983-257346f68c52	hoangnguyenvu1420@gmail.com	HOANGNGUYENVU1420@GMAIL.COM	hoangnguyenvu1420@gmail.com	HOANGNGUYENVU1420@GMAIL.COM	f	AQAAAAIAAYagAAAAEEbkDQhqBC2mhcO2i31fysL/pETG0g8Y5zznoefXLKPtIRh638ZomuQnal+d0apElQ==	GWVIDVECYMHA6YBWV2NWUV76K6WAE7WN	6256e5af-3553-482e-9d03-b54c55a2b484	\N	f	f	\N	t	0
0199d2f9-7213-7896-8b7c-490984904041	hoangnguyenvu1220@gmail.com	HOANGNGUYENVU1220@GMAIL.COM	hoangnguyenvu1220@gmail.com	HOANGNGUYENVU1220@GMAIL.COM	f	AQAAAAIAAYagAAAAEMOBzZFYDmB1r5FOpr5vEDLf9eBXIgXAqogUFFO4R1gMFrjGut6KEZz9gq+OC81abw==	KWQYSTBS24AGETYUR64FXAOCIG54LJGO	e5f29ac0-b2e6-4626-9aca-8bfd2a9ae98d	\N	f	f	\N	t	0
\.


--
-- Data for Name: AspNetUserClaims; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetUserClaims" ("Id", "UserId", "ClaimType", "ClaimValue") FROM stdin;
\.


--
-- Data for Name: AspNetUserLogins; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetUserLogins" ("LoginProvider", "ProviderKey", "ProviderDisplayName", "UserId") FROM stdin;
\.


--
-- Data for Name: AspNetUserRoles; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetUserRoles" ("UserId", "RoleId") FROM stdin;
0199d2f9-704a-72c8-ba9d-cfeec6968948	0199d2f9-6f1c-7ac6-bafa-7eaee262e352
0199d2f9-7123-7cbc-9983-257346f68c52	0199d2f9-6f7f-7573-b37e-d33f845f281c
0199d2f9-7213-7896-8b7c-490984904041	0199d2f9-6f7f-7573-b37e-d33f845f281c
\.


--
-- Data for Name: AspNetUserTokens; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."AspNetUserTokens" ("UserId", "LoginProvider", "Name", "Value") FROM stdin;
\.


--
-- Data for Name: CouponUserUsages; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."CouponUserUsages" ("CouponId", "UserId", "UsageCount") FROM stdin;
\.


--
-- Data for Name: Coupons; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Coupons" ("Id", "RestaurantId", "Code", "Description", "Value_Type", "Value_PercentageValue", "Value_FixedAmount_Amount", "Value_FixedAmount_Currency", "Value_FreeItemValue", "AppliesTo_Scope", "AppliesTo_ItemIds", "AppliesTo_CategoryIds", "MinOrderAmount_Amount", "MinOrderAmount_Currency", "ValidityStartDate", "ValidityEndDate", "TotalUsageLimit", "CurrentTotalUsageCount", "IsEnabled", "UsageLimitPerUser", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: CustomizationGroups; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."CustomizationGroups" ("Id", "RestaurantId", "GroupName", "MinSelections", "MaxSelections", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
01bf1b57-c3bc-4f9e-9134-e2aa06415556	7019b300-9f2a-4758-9745-dd71fb74c327	Size	1	1	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
139bfb36-bd6e-4b9f-9762-ea7c24735926	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Extras	0	3	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
1749e15c-d38a-45ea-8b14-c5df51f18b44	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Size	1	1	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
a2e61604-34cc-4c62-907b-63072d74cd6b	7019b300-9f2a-4758-9745-dd71fb74c327	Extras	0	3	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
b2e9c8aa-00c8-4a13-9e69-1c69572b508e	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Size	1	1	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
b9eaed1d-5b53-459f-a34a-c9e462d0bf70	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Extras	0	3	2025-10-11 11:13:07.643279+00	\N	2025-10-11 11:13:07.643279+00	\N	f	\N	\N
\.


--
-- Data for Name: CustomizationChoices; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."CustomizationChoices" ("ChoiceId", "CustomizationGroupId", "Name", "PriceAdjustment_Amount", "PriceAdjustment_Currency", "IsDefault", "DisplayOrder") FROM stdin;
6c26f764-ca78-4386-9734-0a62c78c195e	01bf1b57-c3bc-4f9e-9134-e2aa06415556	Small	0.00	USD	t	1
7260a140-4604-4248-8cf4-f75554e0b1d2	01bf1b57-c3bc-4f9e-9134-e2aa06415556	Large	2.00	USD	f	3
dbe6a1b6-3702-4eb3-9c15-df769130218c	01bf1b57-c3bc-4f9e-9134-e2aa06415556	Medium	1.00	USD	f	2
2c538341-1aad-4bf9-962c-657a964e911d	139bfb36-bd6e-4b9f-9762-ea7c24735926	Extra Cheese	1.50	USD	f	1
4d0d79e0-f9c6-4cb0-8028-671a73d22ee5	139bfb36-bd6e-4b9f-9762-ea7c24735926	Avocado	2.50	USD	f	3
b428c279-c50d-42d0-941e-ebb430a04750	139bfb36-bd6e-4b9f-9762-ea7c24735926	Bacon	2.00	USD	f	2
e6baa050-62d3-4c6e-bf7a-71a89637fc47	139bfb36-bd6e-4b9f-9762-ea7c24735926	Mushrooms	1.00	USD	f	4
0907bc0e-2d5d-4f8a-ac9d-f02a4e1b22c3	1749e15c-d38a-45ea-8b14-c5df51f18b44	Medium	1.00	USD	f	2
7b09d1c4-03e4-41a3-bf80-d1a83065463b	1749e15c-d38a-45ea-8b14-c5df51f18b44	Small	0.00	USD	t	1
8e4f9982-d140-47b7-b061-d843b6bd242c	1749e15c-d38a-45ea-8b14-c5df51f18b44	Large	2.00	USD	f	3
20a71055-7850-40e9-9f91-38ec7b2e0383	a2e61604-34cc-4c62-907b-63072d74cd6b	Extra Cheese	1.50	USD	f	1
3a95e611-67e0-4121-a2d8-e088aa0509f0	a2e61604-34cc-4c62-907b-63072d74cd6b	Avocado	2.50	USD	f	3
642bc91c-3635-45d7-a986-eed3ef3f46f0	a2e61604-34cc-4c62-907b-63072d74cd6b	Mushrooms	1.00	USD	f	4
6644a8c9-bc7d-4db8-9959-3f191f28a165	a2e61604-34cc-4c62-907b-63072d74cd6b	Bacon	2.00	USD	f	2
715fd874-87de-4c1d-88ca-a05da90d514d	b2e9c8aa-00c8-4a13-9e69-1c69572b508e	Large	2.00	USD	f	3
7ce411f0-8f1d-4201-bab8-e9e0b191d528	b2e9c8aa-00c8-4a13-9e69-1c69572b508e	Small	0.00	USD	t	1
91fe156c-16cf-4893-9539-4cee806d047f	b2e9c8aa-00c8-4a13-9e69-1c69572b508e	Medium	1.00	USD	f	2
30be953b-9b17-47db-9f7f-aac56afcfd64	b9eaed1d-5b53-459f-a34a-c9e462d0bf70	Bacon	2.00	USD	f	2
7a01cea7-a8a3-4ec9-9515-c330ba1a3a6d	b9eaed1d-5b53-459f-a34a-c9e462d0bf70	Mushrooms	1.00	USD	f	4
b87b3726-99ba-486c-bb4f-bb3be3bb93b9	b9eaed1d-5b53-459f-a34a-c9e462d0bf70	Extra Cheese	1.50	USD	f	1
ebc0d4df-89c7-49e3-8191-bbb4b493b190	b9eaed1d-5b53-459f-a34a-c9e462d0bf70	Avocado	2.50	USD	f	3
\.


--
-- Data for Name: Devices; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Devices" ("Id", "DeviceId", "Platform", "ModelName", "CreatedAt", "UpdatedAt") FROM stdin;
f248a6cf-10e7-4d3c-bea5-7ca58802ab1b	seed-device-1	Android	Seed Device 1	2025-10-11 11:13:06.964+00	2025-10-11 11:13:06.964027+00
9972124f-4634-45a1-ad90-ae0aa57de178	seed-device-2	Android	Seed Device 2	2025-10-11 11:13:07.12311+00	2025-10-11 11:13:07.12311+00
\.


--
-- Data for Name: DomainUsers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."DomainUsers" ("Id", "Name", "Email", "PhoneNumber", "IsActive", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
0199d2f9-7123-7cbc-9983-257346f68c52	User 1	hoangnguyenvu1420@gmail.com	\N	t	2025-10-11 11:13:06.920078+00	0199d2f9-7123-7cbc-9983-257346f68c52	2025-10-11 11:13:06.920078+00	0199d2f9-7123-7cbc-9983-257346f68c52	f	\N	\N
0199d2f9-7213-7896-8b7c-490984904041	User 2	hoangnguyenvu1220@gmail.com	\N	t	2025-10-11 11:13:07.116175+00	0199d2f9-7213-7896-8b7c-490984904041	2025-10-11 11:13:07.116175+00	0199d2f9-7213-7896-8b7c-490984904041	f	\N	\N
\.


--
-- Data for Name: FullMenuViews; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."FullMenuViews" ("RestaurantId", "MenuJson", "LastRebuiltAt") FROM stdin;
ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	{"items": {"byId": {"21c938b3-9866-47d8-89b4-3f40a11cc2b2": {"id": "21c938b3-9866-47d8-89b4-3f40a11cc2b2", "name": "Classic Cheeseburger", "price": {"amount": 11.99, "currency": "USD"}, "imageUrl": "https://example.com/main_burger.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "Beef patty with cheddar and pickles", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "24f8f7f9-08fa-4cac-a2a5-74240948412d": {"id": "24f8f7f9-08fa-4cac-a2a5-74240948412d", "name": "Gelato Trio", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_gelato.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Three seasonal flavors", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "30aedc59-7126-47bf-9e91-8d1d7b73abbd": {"id": "30aedc59-7126-47bf-9e91-8d1d7b73abbd", "name": "Brownie Sundae", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_brownie.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Fudge brownie, ice cream, nuts", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "34832c9a-44aa-4e0e-9ec6-5cdcce329554": {"id": "34832c9a-44aa-4e0e-9ec6-5cdcce329554", "name": "Garlic Parmesan Wings", "price": {"amount": 9.49, "currency": "USD"}, "imageUrl": "https://example.com/app_wings.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Crispy wings tossed in garlic parmesan", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "3dcf1522-5b98-450a-817b-8cf99d732da7": {"id": "3dcf1522-5b98-450a-817b-8cf99d732da7", "name": "Grilled Salmon", "price": {"amount": 17.49, "currency": "USD"}, "imageUrl": "https://example.com/main_salmon.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "Lemon herb butter, seasonal veggies", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "42816823-c188-4bb7-8800-d78c475dc468": {"id": "42816823-c188-4bb7-8800-d78c475dc468", "name": "Stuffed Mushrooms", "price": {"amount": 8.49, "currency": "USD"}, "imageUrl": "https://example.com/app_mushrooms.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Herb cream cheese, baked", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "4295aed6-65c3-4868-805b-8eea625ab2d3": {"id": "4295aed6-65c3-4868-805b-8eea625ab2d3", "name": "Coffee", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_coffee.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Freshly brewed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "52beb617-39b9-4f83-b633-dc5693834c95": {"id": "52beb617-39b9-4f83-b633-dc5693834c95", "name": "Tiramisu", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_tiramisu.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Espresso-soaked ladyfingers, mascarpone", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "6b7458d1-dba6-484a-87af-68e0d822dec1": {"id": "6b7458d1-dba6-484a-87af-68e0d822dec1", "name": "Chocolate Lava Cake", "price": {"amount": 7.49, "currency": "USD"}, "imageUrl": "https://example.com/des_lava.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Warm center, vanilla ice cream", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "6cd0b99c-5e44-45f2-901a-f2ecfd1e9c55": {"id": "6cd0b99c-5e44-45f2-901a-f2ecfd1e9c55", "name": "Espresso", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_espresso.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Double shot", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "803cadb4-778b-406c-927e-9c25990323be": {"id": "803cadb4-778b-406c-927e-9c25990323be", "name": "Spinach Artichoke Dip", "price": {"amount": 8.99, "currency": "USD"}, "imageUrl": "https://example.com/app_spinach.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Creamy dip with tortilla chips", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "8f060a14-13b5-4975-b53f-a1b94323a6cd": {"id": "8f060a14-13b5-4975-b53f-a1b94323a6cd", "name": "Apple Pie", "price": {"amount": 5.99, "currency": "USD"}, "imageUrl": "https://example.com/des_applepie.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Cinnamon crumb topping", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "92a026ff-8a3d-40c4-819c-1612d27de491": {"id": "92a026ff-8a3d-40c4-819c-1612d27de491", "name": "Cola", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_cola.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Classic soda", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "998a2068-9c33-440b-8703-91e8ae26ab55": {"id": "998a2068-9c33-440b-8703-91e8ae26ab55", "name": "Fried Calamari", "price": {"amount": 10.99, "currency": "USD"}, "imageUrl": "https://example.com/app_calamari.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Lightly breaded rings with marinara", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "a024ef34-2032-4692-b7f2-9fdaea3dfa67": {"id": "a024ef34-2032-4692-b7f2-9fdaea3dfa67", "name": "Caprese Skewers", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/app_caprese.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Tomato, mozzarella, basil drizzle", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "b1214309-a149-43e9-8140-d678c5989d03": {"id": "b1214309-a149-43e9-8140-d678c5989d03", "name": "Iced Tea", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_icedtea.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Unsweetened black tea", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "c88f9fca-c7de-4bdd-a6a4-ae1d563f3f4c": {"id": "c88f9fca-c7de-4bdd-a6a4-ae1d563f3f4c", "name": "Bruschetta", "price": {"amount": 7.99, "currency": "USD"}, "imageUrl": "https://example.com/app_bruschetta.jpg", "categoryId": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "description": "Grilled bread with tomato, basil, and garlic", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "cee61414-3d95-496b-be96-5c614a2a2fb9": {"id": "cee61414-3d95-496b-be96-5c614a2a2fb9", "name": "Margherita Pizza", "price": {"amount": 12.99, "currency": "USD"}, "imageUrl": "https://example.com/main_margherita.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "Tomato, mozzarella, basil", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "d2a1653e-2dc4-4c0d-822f-406e1485626a": {"id": "d2a1653e-2dc4-4c0d-822f-406e1485626a", "name": "House Lemonade", "price": {"amount": 3.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_lemonade.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Fresh squeezed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "d729e7e0-0d5f-42a2-8446-f7468f6c9f17": {"id": "d729e7e0-0d5f-42a2-8446-f7468f6c9f17", "name": "Chicken Alfredo Pasta", "price": {"amount": 14.49, "currency": "USD"}, "imageUrl": "https://example.com/main_alfredo.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "Creamy parmesan sauce, fettuccine", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "e990e017-f559-4537-bfcb-cca8eb7e2ec9": {"id": "e990e017-f559-4537-bfcb-cca8eb7e2ec9", "name": "Cheesecake", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_cheesecake.jpg", "categoryId": "80dca193-efd7-4cff-ad04-1df19a057f43", "description": "Classic New York style", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "ee82f106-49d5-4df3-b5f2-a1c3879b0ac6": {"id": "ee82f106-49d5-4df3-b5f2-a1c3879b0ac6", "name": "BBQ Chicken Pizza", "price": {"amount": 13.99, "currency": "USD"}, "imageUrl": "https://example.com/main_bbq.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "BBQ sauce, chicken, red onion", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "f262c9a1-cf0a-4fb5-8a9e-9a591fba0347": {"id": "f262c9a1-cf0a-4fb5-8a9e-9a591fba0347", "name": "Sparkling Water", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_sparkling.jpg", "categoryId": "e840ec39-dbc5-4326-8002-941964a95a59", "description": "Chilled with lemon", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "fa2bae82-ffb8-41dd-90ce-f950cff7d1e2": {"id": "fa2bae82-ffb8-41dd-90ce-f950cff7d1e2", "name": "Veggie Stir Fry", "price": {"amount": 12.49, "currency": "USD"}, "imageUrl": "https://example.com/main_stirfry.jpg", "categoryId": "19c9f96e-d511-4f5c-986c-e7956b043829", "description": "Mixed vegetables with soy-ginger glaze", "isAvailable": true, "dietaryTagIds": ["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"], "customizationGroups": []}}}, "menuId": "6b650dc8-f745-45a1-b1cf-58345981ddc1", "version": 1, "currency": "USD", "menuName": "Main Menu", "tagLegend": {"byId": {"10e5d13d-b9b6-4094-a1d6-d9f507f94397": {"name": "Vegan", "category": "Dietary"}, "ac707753-52f0-49a7-bf5d-97699986a0fa": {"name": "Vegetarian", "category": "Dietary"}, "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf": {"name": "Gluten-Free", "category": "Dietary"}}}, "categories": {"byId": {"19c9f96e-d511-4f5c-986c-e7956b043829": {"id": "19c9f96e-d511-4f5c-986c-e7956b043829", "name": "Mains", "itemOrder": ["ee82f106-49d5-4df3-b5f2-a1c3879b0ac6", "d729e7e0-0d5f-42a2-8446-f7468f6c9f17", "21c938b3-9866-47d8-89b4-3f40a11cc2b2", "3dcf1522-5b98-450a-817b-8cf99d732da7", "cee61414-3d95-496b-be96-5c614a2a2fb9", "fa2bae82-ffb8-41dd-90ce-f950cff7d1e2"], "displayOrder": 2}, "80dca193-efd7-4cff-ad04-1df19a057f43": {"id": "80dca193-efd7-4cff-ad04-1df19a057f43", "name": "Desserts", "itemOrder": ["8f060a14-13b5-4975-b53f-a1b94323a6cd", "30aedc59-7126-47bf-9e91-8d1d7b73abbd", "e990e017-f559-4537-bfcb-cca8eb7e2ec9", "6b7458d1-dba6-484a-87af-68e0d822dec1", "24f8f7f9-08fa-4cac-a2a5-74240948412d", "52beb617-39b9-4f83-b633-dc5693834c95"], "displayOrder": 3}, "e840ec39-dbc5-4326-8002-941964a95a59": {"id": "e840ec39-dbc5-4326-8002-941964a95a59", "name": "Drinks", "itemOrder": ["4295aed6-65c3-4868-805b-8eea625ab2d3", "92a026ff-8a3d-40c4-819c-1612d27de491", "6cd0b99c-5e44-45f2-901a-f2ecfd1e9c55", "d2a1653e-2dc4-4c0d-822f-406e1485626a", "b1214309-a149-43e9-8140-d678c5989d03", "f262c9a1-cf0a-4fb5-8a9e-9a591fba0347"], "displayOrder": 4}, "f9811a54-a254-4b85-96fe-ed1fc5c78383": {"id": "f9811a54-a254-4b85-96fe-ed1fc5c78383", "name": "Appetizers", "itemOrder": ["c88f9fca-c7de-4bdd-a6a4-ae1d563f3f4c", "a024ef34-2032-4692-b7f2-9fdaea3dfa67", "998a2068-9c33-440b-8703-91e8ae26ab55", "34832c9a-44aa-4e0e-9ec6-5cdcce329554", "803cadb4-778b-406c-927e-9c25990323be", "42816823-c188-4bb7-8800-d78c475dc468"], "displayOrder": 1}}, "order": ["f9811a54-a254-4b85-96fe-ed1fc5c78383", "19c9f96e-d511-4f5c-986c-e7956b043829", "80dca193-efd7-4cff-ad04-1df19a057f43", "e840ec39-dbc5-4326-8002-941964a95a59"]}, "menuEnabled": true, "restaurantId": "ddf42b3d-dedc-4df0-98ea-bf1fa717cd88", "lastRebuiltAt": "2025-10-11T11:13:18.6672004+00:00", "menuDescription": "Our delicious offerings", "customizationGroups": {"byId": {}}}	2025-10-11 11:13:18.6672+00
7019b300-9f2a-4758-9745-dd71fb74c327	{"items": {"byId": {"196328e1-9ce5-4ae6-93b5-d2f49f14f0e5": {"id": "196328e1-9ce5-4ae6-93b5-d2f49f14f0e5", "name": "Sparkling Water", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_sparkling.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Chilled with lemon", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "23a13f17-afc8-4696-bfc4-03de99e447a6": {"id": "23a13f17-afc8-4696-bfc4-03de99e447a6", "name": "Fried Calamari", "price": {"amount": 10.99, "currency": "USD"}, "imageUrl": "https://example.com/app_calamari.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Lightly breaded rings with marinara", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "38cc668e-53f5-452e-b778-d12c9123602f": {"id": "38cc668e-53f5-452e-b778-d12c9123602f", "name": "Margherita Pizza", "price": {"amount": 12.99, "currency": "USD"}, "imageUrl": "https://example.com/main_margherita.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "Tomato, mozzarella, basil", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "3a4951fd-7591-4568-8053-ff98dce59f80": {"id": "3a4951fd-7591-4568-8053-ff98dce59f80", "name": "Coffee", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_coffee.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Freshly brewed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "454710c5-a758-4008-a3fa-055942213e12": {"id": "454710c5-a758-4008-a3fa-055942213e12", "name": "Stuffed Mushrooms", "price": {"amount": 8.49, "currency": "USD"}, "imageUrl": "https://example.com/app_mushrooms.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Herb cream cheese, baked", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "4a36de13-59f1-47fc-9809-2e60c45c5f05": {"id": "4a36de13-59f1-47fc-9809-2e60c45c5f05", "name": "Chicken Alfredo Pasta", "price": {"amount": 14.49, "currency": "USD"}, "imageUrl": "https://example.com/main_alfredo.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "Creamy parmesan sauce, fettuccine", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "598398ba-e8ce-4627-9fb1-b71561a0c63c": {"id": "598398ba-e8ce-4627-9fb1-b71561a0c63c", "name": "Garlic Parmesan Wings", "price": {"amount": 9.49, "currency": "USD"}, "imageUrl": "https://example.com/app_wings.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Crispy wings tossed in garlic parmesan", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "6e804fcb-30a9-41ba-a1d8-571cb12aa302": {"id": "6e804fcb-30a9-41ba-a1d8-571cb12aa302", "name": "Bruschetta", "price": {"amount": 7.99, "currency": "USD"}, "imageUrl": "https://example.com/app_bruschetta.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Grilled bread with tomato, basil, and garlic", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "7006f065-3305-4722-9671-c575c939c6d9": {"id": "7006f065-3305-4722-9671-c575c939c6d9", "name": "Caprese Skewers", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/app_caprese.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Tomato, mozzarella, basil drizzle", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "8c6a50f8-db42-4683-8309-29c8f8fa1693": {"id": "8c6a50f8-db42-4683-8309-29c8f8fa1693", "name": "Classic Cheeseburger", "price": {"amount": 11.99, "currency": "USD"}, "imageUrl": "https://example.com/main_burger.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "Beef patty with cheddar and pickles", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "9bcea685-a5f5-40a5-ba5f-ac90aa65605c": {"id": "9bcea685-a5f5-40a5-ba5f-ac90aa65605c", "name": "Brownie Sundae", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_brownie.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Fudge brownie, ice cream, nuts", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "9fd1662b-7862-443e-82a5-d41a69abf14d": {"id": "9fd1662b-7862-443e-82a5-d41a69abf14d", "name": "Iced Tea", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_icedtea.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Unsweetened black tea", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "a6b616f9-4017-416a-89d0-e6c4ba9b7e6e": {"id": "a6b616f9-4017-416a-89d0-e6c4ba9b7e6e", "name": "Veggie Stir Fry", "price": {"amount": 12.49, "currency": "USD"}, "imageUrl": "https://example.com/main_stirfry.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "Mixed vegetables with soy-ginger glaze", "isAvailable": true, "dietaryTagIds": ["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"], "customizationGroups": []}, "ab9ef792-f1d8-424d-a642-423bb1c58ca9": {"id": "ab9ef792-f1d8-424d-a642-423bb1c58ca9", "name": "Cola", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_cola.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Classic soda", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "b7005f8f-de25-4179-a244-1f63ef0f604f": {"id": "b7005f8f-de25-4179-a244-1f63ef0f604f", "name": "Apple Pie", "price": {"amount": 5.99, "currency": "USD"}, "imageUrl": "https://example.com/des_applepie.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Cinnamon crumb topping", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "b95d70d8-a4c4-4490-b28b-7906bd84f5e6": {"id": "b95d70d8-a4c4-4490-b28b-7906bd84f5e6", "name": "House Lemonade", "price": {"amount": 3.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_lemonade.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Fresh squeezed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "bb14d2fe-861f-4761-b987-3b06a3d51b4f": {"id": "bb14d2fe-861f-4761-b987-3b06a3d51b4f", "name": "BBQ Chicken Pizza", "price": {"amount": 13.99, "currency": "USD"}, "imageUrl": "https://example.com/main_bbq.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "BBQ sauce, chicken, red onion", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "bea341bd-7359-4643-91c7-dc6e91224b2c": {"id": "bea341bd-7359-4643-91c7-dc6e91224b2c", "name": "Cheesecake", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_cheesecake.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Classic New York style", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "c2358f9d-57d2-4f57-8f6e-faa0acce14d1": {"id": "c2358f9d-57d2-4f57-8f6e-faa0acce14d1", "name": "Spinach Artichoke Dip", "price": {"amount": 8.99, "currency": "USD"}, "imageUrl": "https://example.com/app_spinach.jpg", "categoryId": "6c810004-024f-4857-9afb-6125e3e95ddc", "description": "Creamy dip with tortilla chips", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "d31dc458-2b6d-4f36-a3c3-fdcc400cc732": {"id": "d31dc458-2b6d-4f36-a3c3-fdcc400cc732", "name": "Grilled Salmon", "price": {"amount": 17.49, "currency": "USD"}, "imageUrl": "https://example.com/main_salmon.jpg", "categoryId": "daefcc75-6baf-41ae-b696-2ecc3c988182", "description": "Lemon herb butter, seasonal veggies", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "decd3cdb-02b7-4a2b-85d0-e08f0cb9fc45": {"id": "decd3cdb-02b7-4a2b-85d0-e08f0cb9fc45", "name": "Tiramisu", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_tiramisu.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Espresso-soaked ladyfingers, mascarpone", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "e846b7f3-5bea-49ee-a073-d39b2387fbc3": {"id": "e846b7f3-5bea-49ee-a073-d39b2387fbc3", "name": "Chocolate Lava Cake", "price": {"amount": 7.49, "currency": "USD"}, "imageUrl": "https://example.com/des_lava.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Warm center, vanilla ice cream", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "ef7cdc35-2bea-49db-a6ae-6ac16e76cf78": {"id": "ef7cdc35-2bea-49db-a6ae-6ac16e76cf78", "name": "Gelato Trio", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_gelato.jpg", "categoryId": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "description": "Three seasonal flavors", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "fb6089de-84bb-499f-b780-366129edf21d": {"id": "fb6089de-84bb-499f-b780-366129edf21d", "name": "Espresso", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_espresso.jpg", "categoryId": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "description": "Double shot", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}}}, "menuId": "36efd0f3-b23a-472e-81c5-c0c820879aa7", "version": 1, "currency": "USD", "menuName": "Main Menu", "tagLegend": {"byId": {"10e5d13d-b9b6-4094-a1d6-d9f507f94397": {"name": "Vegan", "category": "Dietary"}, "ac707753-52f0-49a7-bf5d-97699986a0fa": {"name": "Vegetarian", "category": "Dietary"}, "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf": {"name": "Gluten-Free", "category": "Dietary"}}}, "categories": {"byId": {"6c810004-024f-4857-9afb-6125e3e95ddc": {"id": "6c810004-024f-4857-9afb-6125e3e95ddc", "name": "Appetizers", "itemOrder": ["6e804fcb-30a9-41ba-a1d8-571cb12aa302", "7006f065-3305-4722-9671-c575c939c6d9", "23a13f17-afc8-4696-bfc4-03de99e447a6", "598398ba-e8ce-4627-9fb1-b71561a0c63c", "c2358f9d-57d2-4f57-8f6e-faa0acce14d1", "454710c5-a758-4008-a3fa-055942213e12"], "displayOrder": 1}, "c81d6d44-820b-4f9d-beb8-b3233faefcb5": {"id": "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "name": "Desserts", "itemOrder": ["b7005f8f-de25-4179-a244-1f63ef0f604f", "9bcea685-a5f5-40a5-ba5f-ac90aa65605c", "bea341bd-7359-4643-91c7-dc6e91224b2c", "e846b7f3-5bea-49ee-a073-d39b2387fbc3", "ef7cdc35-2bea-49db-a6ae-6ac16e76cf78", "decd3cdb-02b7-4a2b-85d0-e08f0cb9fc45"], "displayOrder": 3}, "daefcc75-6baf-41ae-b696-2ecc3c988182": {"id": "daefcc75-6baf-41ae-b696-2ecc3c988182", "name": "Mains", "itemOrder": ["bb14d2fe-861f-4761-b987-3b06a3d51b4f", "4a36de13-59f1-47fc-9809-2e60c45c5f05", "8c6a50f8-db42-4683-8309-29c8f8fa1693", "d31dc458-2b6d-4f36-a3c3-fdcc400cc732", "38cc668e-53f5-452e-b778-d12c9123602f", "a6b616f9-4017-416a-89d0-e6c4ba9b7e6e"], "displayOrder": 2}, "fc2a6c04-e7c6-467a-aab6-3516b0914d6a": {"id": "fc2a6c04-e7c6-467a-aab6-3516b0914d6a", "name": "Drinks", "itemOrder": ["3a4951fd-7591-4568-8053-ff98dce59f80", "ab9ef792-f1d8-424d-a642-423bb1c58ca9", "fb6089de-84bb-499f-b780-366129edf21d", "b95d70d8-a4c4-4490-b28b-7906bd84f5e6", "9fd1662b-7862-443e-82a5-d41a69abf14d", "196328e1-9ce5-4ae6-93b5-d2f49f14f0e5"], "displayOrder": 4}}, "order": ["6c810004-024f-4857-9afb-6125e3e95ddc", "daefcc75-6baf-41ae-b696-2ecc3c988182", "c81d6d44-820b-4f9d-beb8-b3233faefcb5", "fc2a6c04-e7c6-467a-aab6-3516b0914d6a"]}, "menuEnabled": true, "restaurantId": "7019b300-9f2a-4758-9745-dd71fb74c327", "lastRebuiltAt": "2025-10-11T11:13:18.6671959+00:00", "menuDescription": "Our delicious offerings", "customizationGroups": {"byId": {}}}	2025-10-11 11:13:18.667195+00
5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	{"items": {"byId": {"047dc237-1270-41e5-bc65-d2d8dd032f59": {"id": "047dc237-1270-41e5-bc65-d2d8dd032f59", "name": "Tiramisu", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_tiramisu.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Espresso-soaked ladyfingers, mascarpone", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "10505f7f-4bb3-45b4-b5ee-9d9507a78854": {"id": "10505f7f-4bb3-45b4-b5ee-9d9507a78854", "name": "Cola", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_cola.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Classic soda", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "1f82bf13-e864-4b4e-a6d3-40f413d7a3fa": {"id": "1f82bf13-e864-4b4e-a6d3-40f413d7a3fa", "name": "Grilled Salmon", "price": {"amount": 17.49, "currency": "USD"}, "imageUrl": "https://example.com/main_salmon.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "Lemon herb butter, seasonal veggies", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "3b99344d-be66-408c-857f-6c4f74b1997b": {"id": "3b99344d-be66-408c-857f-6c4f74b1997b", "name": "Chicken Alfredo Pasta", "price": {"amount": 14.49, "currency": "USD"}, "imageUrl": "https://example.com/main_alfredo.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "Creamy parmesan sauce, fettuccine", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "3ecaa164-e3fe-4d0b-b80e-20d990e32300": {"id": "3ecaa164-e3fe-4d0b-b80e-20d990e32300", "name": "Espresso", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_espresso.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Double shot", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "58547858-f9ec-4168-951d-69097bd4fae7": {"id": "58547858-f9ec-4168-951d-69097bd4fae7", "name": "Bruschetta", "price": {"amount": 7.99, "currency": "USD"}, "imageUrl": "https://example.com/app_bruschetta.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Grilled bread with tomato, basil, and garlic", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "5ca19a50-b650-433e-9fec-5120e6f32474": {"id": "5ca19a50-b650-433e-9fec-5120e6f32474", "name": "Margherita Pizza", "price": {"amount": 12.99, "currency": "USD"}, "imageUrl": "https://example.com/main_margherita.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "Tomato, mozzarella, basil", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "74680349-bc5b-4b5f-a8cc-6e24f8858060": {"id": "74680349-bc5b-4b5f-a8cc-6e24f8858060", "name": "Fried Calamari", "price": {"amount": 10.99, "currency": "USD"}, "imageUrl": "https://example.com/app_calamari.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Lightly breaded rings with marinara", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "83a6e99e-7393-4b2a-a023-6f93d55d229c": {"id": "83a6e99e-7393-4b2a-a023-6f93d55d229c", "name": "Caprese Skewers", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/app_caprese.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Tomato, mozzarella, basil drizzle", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "88e4094f-4575-4654-9f20-2ae176f8eb8c": {"id": "88e4094f-4575-4654-9f20-2ae176f8eb8c", "name": "Coffee", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_coffee.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Freshly brewed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "a54ecbbc-4cfc-439a-9835-f2bbfdbdcff1": {"id": "a54ecbbc-4cfc-439a-9835-f2bbfdbdcff1", "name": "Veggie Stir Fry", "price": {"amount": 12.49, "currency": "USD"}, "imageUrl": "https://example.com/main_stirfry.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "Mixed vegetables with soy-ginger glaze", "isAvailable": true, "dietaryTagIds": ["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"], "customizationGroups": []}, "ab7e62f7-385d-491f-995b-4eef9e80d920": {"id": "ab7e62f7-385d-491f-995b-4eef9e80d920", "name": "Gelato Trio", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_gelato.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Three seasonal flavors", "isAvailable": true, "dietaryTagIds": ["ac707753-52f0-49a7-bf5d-97699986a0fa"], "customizationGroups": []}, "b3a67239-0aed-4eeb-b575-8667462bcd2f": {"id": "b3a67239-0aed-4eeb-b575-8667462bcd2f", "name": "Garlic Parmesan Wings", "price": {"amount": 9.49, "currency": "USD"}, "imageUrl": "https://example.com/app_wings.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Crispy wings tossed in garlic parmesan", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "b537baf9-6b33-4e7f-809c-623d54c673ef": {"id": "b537baf9-6b33-4e7f-809c-623d54c673ef", "name": "Apple Pie", "price": {"amount": 5.99, "currency": "USD"}, "imageUrl": "https://example.com/des_applepie.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Cinnamon crumb topping", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "b8653a52-7863-43a1-9c69-717dab9592bc": {"id": "b8653a52-7863-43a1-9c69-717dab9592bc", "name": "Sparkling Water", "price": {"amount": 2.49, "currency": "USD"}, "imageUrl": "https://example.com/dr_sparkling.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Chilled with lemon", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "cd08bf0e-b435-4100-9426-73a4822195ac": {"id": "cd08bf0e-b435-4100-9426-73a4822195ac", "name": "Spinach Artichoke Dip", "price": {"amount": 8.99, "currency": "USD"}, "imageUrl": "https://example.com/app_spinach.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Creamy dip with tortilla chips", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "cdaf5d43-ec68-4221-abcc-26ffb02ab654": {"id": "cdaf5d43-ec68-4221-abcc-26ffb02ab654", "name": "Cheesecake", "price": {"amount": 6.99, "currency": "USD"}, "imageUrl": "https://example.com/des_cheesecake.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Classic New York style", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "d5037afa-ede0-4f3a-b8b0-9e8ff6fa34e8": {"id": "d5037afa-ede0-4f3a-b8b0-9e8ff6fa34e8", "name": "Classic Cheeseburger", "price": {"amount": 11.99, "currency": "USD"}, "imageUrl": "https://example.com/main_burger.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "Beef patty with cheddar and pickles", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "d5c9f786-d7cf-4ee7-926c-5a97f0d4879a": {"id": "d5c9f786-d7cf-4ee7-926c-5a97f0d4879a", "name": "Chocolate Lava Cake", "price": {"amount": 7.49, "currency": "USD"}, "imageUrl": "https://example.com/des_lava.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Warm center, vanilla ice cream", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "dfed149b-69b5-489b-bf64-df97d42e9fcc": {"id": "dfed149b-69b5-489b-bf64-df97d42e9fcc", "name": "Iced Tea", "price": {"amount": 2.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_icedtea.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Unsweetened black tea", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "e5b17d35-247b-4163-8ee3-6481005e4c39": {"id": "e5b17d35-247b-4163-8ee3-6481005e4c39", "name": "Stuffed Mushrooms", "price": {"amount": 8.49, "currency": "USD"}, "imageUrl": "https://example.com/app_mushrooms.jpg", "categoryId": "e10e1431-7883-4a54-b301-cf1e1984ee17", "description": "Herb cream cheese, baked", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "eb15247f-a1e3-44d4-b086-e68fb329ad83": {"id": "eb15247f-a1e3-44d4-b086-e68fb329ad83", "name": "Brownie Sundae", "price": {"amount": 6.49, "currency": "USD"}, "imageUrl": "https://example.com/des_brownie.jpg", "categoryId": "39142c75-644a-4515-8741-8420331c48c6", "description": "Fudge brownie, ice cream, nuts", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "f5514f09-9109-4906-8d27-e76bd0269e4e": {"id": "f5514f09-9109-4906-8d27-e76bd0269e4e", "name": "House Lemonade", "price": {"amount": 3.99, "currency": "USD"}, "imageUrl": "https://example.com/dr_lemonade.jpg", "categoryId": "079da2bf-5e66-4033-b372-75b365639437", "description": "Fresh squeezed", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}, "ff993a63-2a6d-41a9-864d-d122fcbc7e4f": {"id": "ff993a63-2a6d-41a9-864d-d122fcbc7e4f", "name": "BBQ Chicken Pizza", "price": {"amount": 13.99, "currency": "USD"}, "imageUrl": "https://example.com/main_bbq.jpg", "categoryId": "91ed9852-0948-41e9-9be6-76cccfde1365", "description": "BBQ sauce, chicken, red onion", "isAvailable": true, "dietaryTagIds": [], "customizationGroups": []}}}, "menuId": "0e89169b-748d-41fa-9790-6997a3f3802f", "version": 1, "currency": "USD", "menuName": "Main Menu", "tagLegend": {"byId": {"10e5d13d-b9b6-4094-a1d6-d9f507f94397": {"name": "Vegan", "category": "Dietary"}, "ac707753-52f0-49a7-bf5d-97699986a0fa": {"name": "Vegetarian", "category": "Dietary"}, "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf": {"name": "Gluten-Free", "category": "Dietary"}}}, "categories": {"byId": {"079da2bf-5e66-4033-b372-75b365639437": {"id": "079da2bf-5e66-4033-b372-75b365639437", "name": "Drinks", "itemOrder": ["88e4094f-4575-4654-9f20-2ae176f8eb8c", "10505f7f-4bb3-45b4-b5ee-9d9507a78854", "3ecaa164-e3fe-4d0b-b80e-20d990e32300", "f5514f09-9109-4906-8d27-e76bd0269e4e", "dfed149b-69b5-489b-bf64-df97d42e9fcc", "b8653a52-7863-43a1-9c69-717dab9592bc"], "displayOrder": 4}, "39142c75-644a-4515-8741-8420331c48c6": {"id": "39142c75-644a-4515-8741-8420331c48c6", "name": "Desserts", "itemOrder": ["b537baf9-6b33-4e7f-809c-623d54c673ef", "eb15247f-a1e3-44d4-b086-e68fb329ad83", "cdaf5d43-ec68-4221-abcc-26ffb02ab654", "d5c9f786-d7cf-4ee7-926c-5a97f0d4879a", "ab7e62f7-385d-491f-995b-4eef9e80d920", "047dc237-1270-41e5-bc65-d2d8dd032f59"], "displayOrder": 3}, "91ed9852-0948-41e9-9be6-76cccfde1365": {"id": "91ed9852-0948-41e9-9be6-76cccfde1365", "name": "Mains", "itemOrder": ["ff993a63-2a6d-41a9-864d-d122fcbc7e4f", "3b99344d-be66-408c-857f-6c4f74b1997b", "d5037afa-ede0-4f3a-b8b0-9e8ff6fa34e8", "1f82bf13-e864-4b4e-a6d3-40f413d7a3fa", "5ca19a50-b650-433e-9fec-5120e6f32474", "a54ecbbc-4cfc-439a-9835-f2bbfdbdcff1"], "displayOrder": 2}, "e10e1431-7883-4a54-b301-cf1e1984ee17": {"id": "e10e1431-7883-4a54-b301-cf1e1984ee17", "name": "Appetizers", "itemOrder": ["58547858-f9ec-4168-951d-69097bd4fae7", "83a6e99e-7393-4b2a-a023-6f93d55d229c", "74680349-bc5b-4b5f-a8cc-6e24f8858060", "b3a67239-0aed-4eeb-b575-8667462bcd2f", "cd08bf0e-b435-4100-9426-73a4822195ac", "e5b17d35-247b-4163-8ee3-6481005e4c39"], "displayOrder": 1}}, "order": ["e10e1431-7883-4a54-b301-cf1e1984ee17", "91ed9852-0948-41e9-9be6-76cccfde1365", "39142c75-644a-4515-8741-8420331c48c6", "079da2bf-5e66-4033-b372-75b365639437"]}, "menuEnabled": true, "restaurantId": "5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a", "lastRebuiltAt": "2025-10-11T11:13:18.6685366+00:00", "menuDescription": "Our delicious offerings", "customizationGroups": {"byId": {}}}	2025-10-11 11:13:18.668536+00
\.


--
-- Data for Name: InboxMessages; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."InboxMessages" ("EventId", "Handler", "ProcessedOnUtc", "Error") FROM stdin;
\.


--
-- Data for Name: MenuCategories; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."MenuCategories" ("Id", "MenuId", "Name", "DisplayOrder", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
079da2bf-5e66-4033-b372-75b365639437	0e89169b-748d-41fa-9790-6997a3f3802f	Drinks	4	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
19c9f96e-d511-4f5c-986c-e7956b043829	6b650dc8-f745-45a1-b1cf-58345981ddc1	Mains	2	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
39142c75-644a-4515-8741-8420331c48c6	0e89169b-748d-41fa-9790-6997a3f3802f	Desserts	3	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
6c810004-024f-4857-9afb-6125e3e95ddc	36efd0f3-b23a-472e-81c5-c0c820879aa7	Appetizers	1	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
80dca193-efd7-4cff-ad04-1df19a057f43	6b650dc8-f745-45a1-b1cf-58345981ddc1	Desserts	3	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
91ed9852-0948-41e9-9be6-76cccfde1365	0e89169b-748d-41fa-9790-6997a3f3802f	Mains	2	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
c81d6d44-820b-4f9d-beb8-b3233faefcb5	36efd0f3-b23a-472e-81c5-c0c820879aa7	Desserts	3	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
daefcc75-6baf-41ae-b696-2ecc3c988182	36efd0f3-b23a-472e-81c5-c0c820879aa7	Mains	2	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
e10e1431-7883-4a54-b301-cf1e1984ee17	0e89169b-748d-41fa-9790-6997a3f3802f	Appetizers	1	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
e840ec39-dbc5-4326-8002-941964a95a59	6b650dc8-f745-45a1-b1cf-58345981ddc1	Drinks	4	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
f9811a54-a254-4b85-96fe-ed1fc5c78383	6b650dc8-f745-45a1-b1cf-58345981ddc1	Appetizers	1	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
fc2a6c04-e7c6-467a-aab6-3516b0914d6a	36efd0f3-b23a-472e-81c5-c0c820879aa7	Drinks	4	2025-10-11 11:13:07.550898+00	\N	2025-10-11 11:13:07.550898+00	\N	f	\N	\N
\.


--
-- Data for Name: MenuItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."MenuItems" ("Id", "RestaurantId", "MenuCategoryId", "Name", "Description", "BasePrice_Amount", "BasePrice_Currency", "ImageUrl", "IsAvailable", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy", "DietaryTagIds", "AppliedCustomizations") FROM stdin;
047dc237-1270-41e5-bc65-d2d8dd032f59	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Tiramisu	Espresso-soaked ladyfingers, mascarpone	6.99	USD	https://example.com/des_tiramisu.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
10505f7f-4bb3-45b4-b5ee-9d9507a78854	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	Cola	Classic soda	2.49	USD	https://example.com/dr_cola.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
196328e1-9ce5-4ae6-93b5-d2f49f14f0e5	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	Sparkling Water	Chilled with lemon	2.49	USD	https://example.com/dr_sparkling.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
1f82bf13-e864-4b4e-a6d3-40f413d7a3fa	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	Grilled Salmon	Lemon herb butter, seasonal veggies	17.49	USD	https://example.com/main_salmon.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
21c938b3-9866-47d8-89b4-3f40a11cc2b2	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	Classic Cheeseburger	Beef patty with cheddar and pickles	11.99	USD	https://example.com/main_burger.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
23a13f17-afc8-4696-bfc4-03de99e447a6	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Fried Calamari	Lightly breaded rings with marinara	10.99	USD	https://example.com/app_calamari.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
24f8f7f9-08fa-4cac-a2a5-74240948412d	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Gelato Trio	Three seasonal flavors	6.49	USD	https://example.com/des_gelato.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
30aedc59-7126-47bf-9e91-8d1d7b73abbd	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Brownie Sundae	Fudge brownie, ice cream, nuts	6.49	USD	https://example.com/des_brownie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
34832c9a-44aa-4e0e-9ec6-5cdcce329554	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	9.49	USD	https://example.com/app_wings.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
38cc668e-53f5-452e-b778-d12c9123602f	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	Margherita Pizza	Tomato, mozzarella, basil	12.99	USD	https://example.com/main_margherita.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
3a4951fd-7591-4568-8053-ff98dce59f80	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	Coffee	Freshly brewed	2.49	USD	https://example.com/dr_coffee.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
3b99344d-be66-408c-857f-6c4f74b1997b	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	14.49	USD	https://example.com/main_alfredo.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
3dcf1522-5b98-450a-817b-8cf99d732da7	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	Grilled Salmon	Lemon herb butter, seasonal veggies	17.49	USD	https://example.com/main_salmon.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
3ecaa164-e3fe-4d0b-b80e-20d990e32300	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	Espresso	Double shot	2.99	USD	https://example.com/dr_espresso.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
42816823-c188-4bb7-8800-d78c475dc468	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Stuffed Mushrooms	Herb cream cheese, baked	8.49	USD	https://example.com/app_mushrooms.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
4295aed6-65c3-4868-805b-8eea625ab2d3	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	Coffee	Freshly brewed	2.49	USD	https://example.com/dr_coffee.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
454710c5-a758-4008-a3fa-055942213e12	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Stuffed Mushrooms	Herb cream cheese, baked	8.49	USD	https://example.com/app_mushrooms.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
4a36de13-59f1-47fc-9809-2e60c45c5f05	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	14.49	USD	https://example.com/main_alfredo.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
52beb617-39b9-4f83-b633-dc5693834c95	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Tiramisu	Espresso-soaked ladyfingers, mascarpone	6.99	USD	https://example.com/des_tiramisu.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
58547858-f9ec-4168-951d-69097bd4fae7	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Bruschetta	Grilled bread with tomato, basil, and garlic	7.99	USD	https://example.com/app_bruschetta.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
598398ba-e8ce-4627-9fb1-b71561a0c63c	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	9.49	USD	https://example.com/app_wings.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
5ca19a50-b650-433e-9fec-5120e6f32474	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	Margherita Pizza	Tomato, mozzarella, basil	12.99	USD	https://example.com/main_margherita.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
6b7458d1-dba6-484a-87af-68e0d822dec1	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Chocolate Lava Cake	Warm center, vanilla ice cream	7.49	USD	https://example.com/des_lava.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
6cd0b99c-5e44-45f2-901a-f2ecfd1e9c55	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	Espresso	Double shot	2.99	USD	https://example.com/dr_espresso.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
6e804fcb-30a9-41ba-a1d8-571cb12aa302	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Bruschetta	Grilled bread with tomato, basil, and garlic	7.99	USD	https://example.com/app_bruschetta.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
7006f065-3305-4722-9671-c575c939c6d9	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Caprese Skewers	Tomato, mozzarella, basil drizzle	6.99	USD	https://example.com/app_caprese.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
74680349-bc5b-4b5f-a8cc-6e24f8858060	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Fried Calamari	Lightly breaded rings with marinara	10.99	USD	https://example.com/app_calamari.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
803cadb4-778b-406c-927e-9c25990323be	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Spinach Artichoke Dip	Creamy dip with tortilla chips	8.99	USD	https://example.com/app_spinach.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
83a6e99e-7393-4b2a-a023-6f93d55d229c	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Caprese Skewers	Tomato, mozzarella, basil drizzle	6.99	USD	https://example.com/app_caprese.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
88e4094f-4575-4654-9f20-2ae176f8eb8c	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	Coffee	Freshly brewed	2.49	USD	https://example.com/dr_coffee.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
8c6a50f8-db42-4683-8309-29c8f8fa1693	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	Classic Cheeseburger	Beef patty with cheddar and pickles	11.99	USD	https://example.com/main_burger.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
8f060a14-13b5-4975-b53f-a1b94323a6cd	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Apple Pie	Cinnamon crumb topping	5.99	USD	https://example.com/des_applepie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
92a026ff-8a3d-40c4-819c-1612d27de491	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	Cola	Classic soda	2.49	USD	https://example.com/dr_cola.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
998a2068-9c33-440b-8703-91e8ae26ab55	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Fried Calamari	Lightly breaded rings with marinara	10.99	USD	https://example.com/app_calamari.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
9bcea685-a5f5-40a5-ba5f-ac90aa65605c	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Brownie Sundae	Fudge brownie, ice cream, nuts	6.49	USD	https://example.com/des_brownie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
9fd1662b-7862-443e-82a5-d41a69abf14d	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	Iced Tea	Unsweetened black tea	2.99	USD	https://example.com/dr_icedtea.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
a024ef34-2032-4692-b7f2-9fdaea3dfa67	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Caprese Skewers	Tomato, mozzarella, basil drizzle	6.99	USD	https://example.com/app_caprese.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
a54ecbbc-4cfc-439a-9835-f2bbfdbdcff1	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	12.49	USD	https://example.com/main_stirfry.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"]	[]
a6b616f9-4017-416a-89d0-e6c4ba9b7e6e	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	12.49	USD	https://example.com/main_stirfry.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"]	[]
ab7e62f7-385d-491f-995b-4eef9e80d920	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Gelato Trio	Three seasonal flavors	6.49	USD	https://example.com/des_gelato.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
ab9ef792-f1d8-424d-a642-423bb1c58ca9	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	Cola	Classic soda	2.49	USD	https://example.com/dr_cola.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b1214309-a149-43e9-8140-d678c5989d03	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	Iced Tea	Unsweetened black tea	2.99	USD	https://example.com/dr_icedtea.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b3a67239-0aed-4eeb-b575-8667462bcd2f	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	9.49	USD	https://example.com/app_wings.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b537baf9-6b33-4e7f-809c-623d54c673ef	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Apple Pie	Cinnamon crumb topping	5.99	USD	https://example.com/des_applepie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b7005f8f-de25-4179-a244-1f63ef0f604f	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Apple Pie	Cinnamon crumb topping	5.99	USD	https://example.com/des_applepie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b8653a52-7863-43a1-9c69-717dab9592bc	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	Sparkling Water	Chilled with lemon	2.49	USD	https://example.com/dr_sparkling.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
b95d70d8-a4c4-4490-b28b-7906bd84f5e6	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	House Lemonade	Fresh squeezed	3.99	USD	https://example.com/dr_lemonade.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
bb14d2fe-861f-4761-b987-3b06a3d51b4f	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	13.99	USD	https://example.com/main_bbq.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
bea341bd-7359-4643-91c7-dc6e91224b2c	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Cheesecake	Classic New York style	6.99	USD	https://example.com/des_cheesecake.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
c2358f9d-57d2-4f57-8f6e-faa0acce14d1	7019b300-9f2a-4758-9745-dd71fb74c327	6c810004-024f-4857-9afb-6125e3e95ddc	Spinach Artichoke Dip	Creamy dip with tortilla chips	8.99	USD	https://example.com/app_spinach.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
c88f9fca-c7de-4bdd-a6a4-ae1d563f3f4c	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	f9811a54-a254-4b85-96fe-ed1fc5c78383	Bruschetta	Grilled bread with tomato, basil, and garlic	7.99	USD	https://example.com/app_bruschetta.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
cd08bf0e-b435-4100-9426-73a4822195ac	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Spinach Artichoke Dip	Creamy dip with tortilla chips	8.99	USD	https://example.com/app_spinach.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
cdaf5d43-ec68-4221-abcc-26ffb02ab654	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Cheesecake	Classic New York style	6.99	USD	https://example.com/des_cheesecake.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
cee61414-3d95-496b-be96-5c614a2a2fb9	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	Margherita Pizza	Tomato, mozzarella, basil	12.99	USD	https://example.com/main_margherita.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
d2a1653e-2dc4-4c0d-822f-406e1485626a	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	House Lemonade	Fresh squeezed	3.99	USD	https://example.com/dr_lemonade.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
d31dc458-2b6d-4f36-a3c3-fdcc400cc732	7019b300-9f2a-4758-9745-dd71fb74c327	daefcc75-6baf-41ae-b696-2ecc3c988182	Grilled Salmon	Lemon herb butter, seasonal veggies	17.49	USD	https://example.com/main_salmon.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
d5037afa-ede0-4f3a-b8b0-9e8ff6fa34e8	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	Classic Cheeseburger	Beef patty with cheddar and pickles	11.99	USD	https://example.com/main_burger.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
d5c9f786-d7cf-4ee7-926c-5a97f0d4879a	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Chocolate Lava Cake	Warm center, vanilla ice cream	7.49	USD	https://example.com/des_lava.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
d729e7e0-0d5f-42a2-8446-f7468f6c9f17	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	14.49	USD	https://example.com/main_alfredo.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
decd3cdb-02b7-4a2b-85d0-e08f0cb9fc45	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Tiramisu	Espresso-soaked ladyfingers, mascarpone	6.99	USD	https://example.com/des_tiramisu.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
dfed149b-69b5-489b-bf64-df97d42e9fcc	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	Iced Tea	Unsweetened black tea	2.99	USD	https://example.com/dr_icedtea.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
e5b17d35-247b-4163-8ee3-6481005e4c39	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	e10e1431-7883-4a54-b301-cf1e1984ee17	Stuffed Mushrooms	Herb cream cheese, baked	8.49	USD	https://example.com/app_mushrooms.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
e846b7f3-5bea-49ee-a073-d39b2387fbc3	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Chocolate Lava Cake	Warm center, vanilla ice cream	7.49	USD	https://example.com/des_lava.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
e990e017-f559-4537-bfcb-cca8eb7e2ec9	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	80dca193-efd7-4cff-ad04-1df19a057f43	Cheesecake	Classic New York style	6.99	USD	https://example.com/des_cheesecake.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
eb15247f-a1e3-44d4-b086-e68fb329ad83	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	39142c75-644a-4515-8741-8420331c48c6	Brownie Sundae	Fudge brownie, ice cream, nuts	6.49	USD	https://example.com/des_brownie.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
ee82f106-49d5-4df3-b5f2-a1c3879b0ac6	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	13.99	USD	https://example.com/main_bbq.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
ef7cdc35-2bea-49db-a6ae-6ac16e76cf78	7019b300-9f2a-4758-9745-dd71fb74c327	c81d6d44-820b-4f9d-beb8-b3233faefcb5	Gelato Trio	Three seasonal flavors	6.49	USD	https://example.com/des_gelato.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["ac707753-52f0-49a7-bf5d-97699986a0fa"]	[]
f262c9a1-cf0a-4fb5-8a9e-9a591fba0347	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	e840ec39-dbc5-4326-8002-941964a95a59	Sparkling Water	Chilled with lemon	2.49	USD	https://example.com/dr_sparkling.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
f5514f09-9109-4906-8d27-e76bd0269e4e	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	079da2bf-5e66-4033-b372-75b365639437	House Lemonade	Fresh squeezed	3.99	USD	https://example.com/dr_lemonade.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
fa2bae82-ffb8-41dd-90ce-f950cff7d1e2	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	19c9f96e-d511-4f5c-986c-e7956b043829	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	12.49	USD	https://example.com/main_stirfry.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	["10e5d13d-b9b6-4094-a1d6-d9f507f94397", "c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf"]	[]
fb6089de-84bb-499f-b780-366129edf21d	7019b300-9f2a-4758-9745-dd71fb74c327	fc2a6c04-e7c6-467a-aab6-3516b0914d6a	Espresso	Double shot	2.99	USD	https://example.com/dr_espresso.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
ff993a63-2a6d-41a9-864d-d122fcbc7e4f	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	91ed9852-0948-41e9-9be6-76cccfde1365	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	13.99	USD	https://example.com/main_bbq.jpg	t	2025-10-11 11:13:07.802033+00	\N	2025-10-11 11:13:07.802033+00	\N	f	\N	\N	[]	[]
\.


--
-- Data for Name: Menus; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Menus" ("Id", "Name", "Description", "IsEnabled", "RestaurantId", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
0e89169b-748d-41fa-9790-6997a3f3802f	Main Menu	Our delicious offerings	t	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	2025-10-11 11:13:07.384784+00	\N	2025-10-11 11:13:07.384784+00	\N	f	\N	\N
36efd0f3-b23a-472e-81c5-c0c820879aa7	Main Menu	Our delicious offerings	t	7019b300-9f2a-4758-9745-dd71fb74c327	2025-10-11 11:13:07.384784+00	\N	2025-10-11 11:13:07.384784+00	\N	f	\N	\N
6b650dc8-f745-45a1-b1cf-58345981ddc1	Main Menu	Our delicious offerings	t	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	2025-10-11 11:13:07.384784+00	\N	2025-10-11 11:13:07.384784+00	\N	f	\N	\N
\.


--
-- Data for Name: Orders; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Orders" ("Id", "Created", "CreatedBy", "OrderNumber", "Status", "PlacementTimestamp", "LastUpdateTimestamp", "EstimatedDeliveryTime", "ActualDeliveryTime", "SpecialInstructions", "DeliveryAddress_Street", "DeliveryAddress_City", "DeliveryAddress_State", "DeliveryAddress_ZipCode", "DeliveryAddress_Country", "Subtotal_Amount", "Subtotal_Currency", "DiscountAmount_Amount", "DiscountAmount_Currency", "DeliveryFee_Amount", "DeliveryFee_Currency", "TipAmount_Amount", "TipAmount_Currency", "TaxAmount_Amount", "TaxAmount_Currency", "TotalAmount_Amount", "TotalAmount_Currency", "CustomerId", "RestaurantId", "SourceTeamCartId", "AppliedCouponId") FROM stdin;
\.


--
-- Data for Name: OrderItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."OrderItems" ("OrderItemId", "OrderId", "Snapshot_MenuCategoryId", "Snapshot_MenuItemId", "Snapshot_ItemName", "BasePrice_Amount", "BasePrice_Currency", "Quantity", "LineItemTotal_Amount", "LineItemTotal_Currency", "SelectedCustomizations") FROM stdin;
\.


--
-- Data for Name: OutboxMessages; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."OutboxMessages" ("Id", "OccurredOnUtc", "Type", "Content", "CorrelationId", "CausationId", "AggregateId", "AggregateType", "Attempt", "NextAttemptOnUtc", "ProcessedOnUtc", "Error") FROM stdin;
\.


--
-- Data for Name: PaymentTransactions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."PaymentTransactions" ("PaymentTransactionId", "OrderId", "PaymentMethodType", "PaymentMethodDisplay", "Type", "Transaction_Amount", "Transaction_Currency", "Status", "Timestamp", "PaymentGatewayReferenceId", "PaidByUserId") FROM stdin;
\.


--
-- Data for Name: ProcessedWebhookEvents; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."ProcessedWebhookEvents" ("Id", "ProcessedAt") FROM stdin;
\.


--
-- Data for Name: RestaurantAccounts; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."RestaurantAccounts" ("Id", "RestaurantId", "CurrentBalance_Amount", "CurrentBalance_Currency", "PayoutMethod_Details", "Created", "CreatedBy") FROM stdin;
\.


--
-- Data for Name: RestaurantRegistrations; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."RestaurantRegistrations" ("Id", "SubmitterUserId", "Name", "Description", "CuisineType", "Street", "City", "State", "ZipCode", "Country", "PhoneNumber", "Email", "BusinessHours", "LogoUrl", "Latitude", "Longitude", "Status", "SubmittedAtUtc", "ReviewedAtUtc", "ReviewedByUserId", "ReviewNote", "Created", "CreatedBy") FROM stdin;
\.


--
-- Data for Name: RestaurantReviewSummaries; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."RestaurantReviewSummaries" ("RestaurantId", "AverageRating", "TotalReviews", "LastReviewAtUtc", "Ratings1", "Ratings2", "Ratings3", "Ratings4", "Ratings5", "TotalWithText", "UpdatedAtUtc") FROM stdin;
5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	0	0	\N	0	0	0	0	0	0	2025-10-11 11:13:18.512676+00
7019b300-9f2a-4758-9745-dd71fb74c327	0	0	\N	0	0	0	0	0	0	2025-10-11 11:13:18.512676+00
ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	0	0	\N	0	0	0	0	0	0	2025-10-11 11:13:18.512676+00
\.


--
-- Data for Name: Restaurants; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Restaurants" ("Id", "Name", "LogoUrl", "BackgroundImageUrl", "Description", "CuisineType", "Location_Street", "Location_City", "Location_State", "Location_ZipCode", "Location_Country", "Geo_Latitude", "Geo_Longitude", "ContactInfo_PhoneNumber", "ContactInfo_Email", "BusinessHours", "IsVerified", "IsAcceptingOrders", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Sakura Sushi	https://example.com/sakura-logo.png	https://example.com/sakura-bg.jpg	Fresh sushi and Japanese favorites	Japanese	456 Cherry Blossom Ave	Midtown	WA	98101	USA	47.6062	-122.3321	+1 (555) 987-6543	hello@sakurasushi.com	10:30-21:30	t	t	2025-10-11 11:13:07.213939+00	\N	2025-10-11 11:13:07.213939+00	\N	f	\N	\N
7019b300-9f2a-4758-9745-dd71fb74c327	El Camino Taqueria	https://example.com/elcamino-logo.png	https://example.com/elcamino-bg.jpg	Street-style tacos and modern Mexican fare	Mexican	789 Fiesta Road	Uptown	TX	73301	USA	30.2672	-97.7431	+1 (555) 222-3344	contact@elcamino.com	11:00-23:00	t	t	2025-10-11 11:13:07.213939+00	\N	2025-10-11 11:13:07.213939+00	\N	f	\N	\N
ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Bella Vista Italian	https://example.com/bella-vista-logo.png	https://example.com/bella-vista-bg.jpg	Authentic Italian cuisine in the heart of downtown	Italian	123 Main Street	Downtown	CA	90210	USA	34.0522	-118.2437	+1 (555) 123-4567	orders@bellavista.com	11:00-22:00	t	t	2025-10-11 11:13:07.213939+00	\N	2025-10-11 11:13:07.213939+00	\N	f	\N	\N
\.


--
-- Data for Name: Reviews; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Reviews" ("Id", "OrderId", "CustomerId", "RestaurantId", "Rating", "Comment", "SubmissionTimestamp", "IsModerated", "IsHidden", "Reply", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
\.


--
-- Data for Name: RoleAssignments; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."RoleAssignments" ("Id", "UserId", "RestaurantId", "Role", "Created", "CreatedBy") FROM stdin;
\.


--
-- Data for Name: SearchIndexItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SearchIndexItems" ("Id", "Type", "RestaurantId", "Name", "Description", "Cuisine", "Tags", "Keywords", "IsOpenNow", "IsAcceptingOrders", "AvgRating", "ReviewCount", "PriceBand", "Geo", "CreatedAt", "UpdatedAt", "SourceVersion", "SoftDeleted", "TsAll", "TsName", "TsDescr") FROM stdin;
5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	restaurant	\N	Sakura Sushi	Fresh sushi and Japanese favorites	Japanese	\N	\N	t	t	0	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.213939+00	2025-10-11 11:13:07.213939+00	638957779988496535	f	'and':6C 'favorites':8C 'fresh':4C 'japanese':3B,7C 'sakura':1A 'sushi':2A,5C	'sakura':1 'sushi':2	'and':3 'favorites':5 'fresh':1 'japanese':4 'sushi':2
9fd1662b-7862-443e-82a5-d41a69abf14d	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Iced Tea	Unsweetened black tea	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989198600	f	'black':5C 'iced':1A 'mexican':3B 'tea':2A,6C 'unsweetened':4C	'iced':1 'tea':2	'black':2 'tea':3 'unsweetened':1
ab9ef792-f1d8-424d-a642-423bb1c58ca9	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Cola	Classic soda	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989210632	f	'classic':3C 'cola':1A 'mexican':2B 'soda':4C	'cola':1	'classic':1 'soda':2
e990e017-f559-4537-bfcb-cca8eb7e2ec9	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Cheesecake	Classic New York style	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990256760	f	'cheesecake':1A 'classic':3C 'italian':2B 'new':4C 'style':6C 'york':5C	'cheesecake':1	'classic':1 'new':2 'style':4 'york':3
d729e7e0-0d5f-42a2-8446-f7468f6c9f17	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990275339	f	'alfredo':2A 'chicken':1A 'creamy':5C 'fettuccine':8C 'italian':4B 'parmesan':6C 'pasta':3A 'sauce':7C	'alfredo':2 'chicken':1 'pasta':3	'creamy':1 'fettuccine':4 'parmesan':2 'sauce':3
24f8f7f9-08fa-4cac-a2a5-74240948412d	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Gelato Trio	Three seasonal flavors	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990292394	f	'flavors':6C 'gelato':1A 'italian':3B 'seasonal':5C 'three':4C 'trio':2A	'gelato':1 'trio':2	'flavors':3 'seasonal':2 'three':1
6b7458d1-dba6-484a-87af-68e0d822dec1	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Chocolate Lava Cake	Warm center, vanilla ice cream	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990312818	f	'cake':3A 'center':6C 'chocolate':1A 'cream':9C 'ice':8C 'italian':4B 'lava':2A 'vanilla':7C 'warm':5C	'cake':3 'chocolate':1 'lava':2	'center':2 'cream':5 'ice':4 'vanilla':3 'warm':1
34832c9a-44aa-4e0e-9ec6-5cdcce329554	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990332781	f	'crispy':5C 'garlic':1A,9C 'in':8C 'italian':4B 'parmesan':2A,10C 'tossed':7C 'wings':3A,6C	'garlic':1 'parmesan':2 'wings':3	'crispy':1 'garlic':5 'in':4 'parmesan':6 'tossed':3 'wings':2
7019b300-9f2a-4758-9745-dd71fb74c327	restaurant	\N	El Camino Taqueria	Street-style tacos and modern Mexican fare	Mexican	\N	\N	t	t	0	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.213939+00	2025-10-11 11:13:07.213939+00	638957779988496534	f	'and':9C 'camino':2A 'el':1A 'fare':12C 'mexican':4B,11C 'modern':10C 'street':6C 'street-style':5C 'style':7C 'tacos':8C 'taqueria':3A	'camino':2 'el':1 'taqueria':3	'and':5 'fare':8 'mexican':7 'modern':6 'street':2 'street-style':1 'style':3 'tacos':4
83a6e99e-7393-4b2a-a023-6f93d55d229c	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Caprese Skewers	Tomato, mozzarella, basil drizzle	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989145678	f	'basil':6C 'caprese':1A 'drizzle':7C 'japanese':3B 'mozzarella':5C 'skewers':2A 'tomato':4C	'caprese':1 'skewers':2	'basil':3 'drizzle':4 'mozzarella':2 'tomato':1
454710c5-a758-4008-a3fa-055942213e12	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Stuffed Mushrooms	Herb cream cheese, baked	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989216522	f	'baked':7C 'cheese':6C 'cream':5C 'herb':4C 'mexican':3B 'mushrooms':2A 'stuffed':1A	'mushrooms':2 'stuffed':1	'baked':4 'cheese':3 'cream':2 'herb':1
decd3cdb-02b7-4a2b-85d0-e08f0cb9fc45	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Tiramisu	Espresso-soaked ladyfingers, mascarpone	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989247420	f	'espresso':4C 'espresso-soaked':3C 'ladyfingers':6C 'mascarpone':7C 'mexican':2B 'soaked':5C 'tiramisu':1A	'tiramisu':1	'espresso':2 'espresso-soaked':1 'ladyfingers':4 'mascarpone':5 'soaked':3
8c6a50f8-db42-4683-8309-29c8f8fa1693	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Classic Cheeseburger	Beef patty with cheddar and pickles	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989298670	f	'and':8C 'beef':4C 'cheddar':7C 'cheeseburger':2A 'classic':1A 'mexican':3B 'patty':5C 'pickles':9C 'with':6C	'cheeseburger':2 'classic':1	'and':5 'beef':1 'cheddar':4 'patty':2 'pickles':6 'with':3
b7005f8f-de25-4179-a244-1f63ef0f604f	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Apple Pie	Cinnamon crumb topping	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989305939	f	'apple':1A 'cinnamon':4C 'crumb':5C 'mexican':3B 'pie':2A 'topping':6C	'apple':1 'pie':2	'cinnamon':1 'crumb':2 'topping':3
b95d70d8-a4c4-4490-b28b-7906bd84f5e6	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	House Lemonade	Fresh squeezed	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989371302	f	'fresh':4C 'house':1A 'lemonade':2A 'mexican':3B 'squeezed':5C	'house':1 'lemonade':2	'fresh':1 'squeezed':2
c2358f9d-57d2-4f57-8f6e-faa0acce14d1	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Spinach Artichoke Dip	Creamy dip with tortilla chips	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989425246	f	'artichoke':2A 'chips':9C 'creamy':5C 'dip':3A,6C 'mexican':4B 'spinach':1A 'tortilla':8C 'with':7C	'artichoke':2 'dip':3 'spinach':1	'chips':5 'creamy':1 'dip':2 'tortilla':4 'with':3
196328e1-9ce5-4ae6-93b5-d2f49f14f0e5	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Sparkling Water	Chilled with lemon	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989432131	f	'chilled':4C 'lemon':6C 'mexican':3B 'sparkling':1A 'water':2A 'with':5C	'sparkling':1 'water':2	'chilled':1 'lemon':3 'with':2
bea341bd-7359-4643-91c7-dc6e91224b2c	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Cheesecake	Classic New York style	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989432048	f	'cheesecake':1A 'classic':3C 'mexican':2B 'new':4C 'style':6C 'york':5C	'cheesecake':1	'classic':1 'new':2 'style':4 'york':3
23a13f17-afc8-4696-bfc4-03de99e447a6	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Fried Calamari	Lightly breaded rings with marinara	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989445335	f	'breaded':5C 'calamari':2A 'fried':1A 'lightly':4C 'marinara':8C 'mexican':3B 'rings':6C 'with':7C	'calamari':2 'fried':1	'breaded':2 'lightly':1 'marinara':5 'rings':3 'with':4
21c938b3-9866-47d8-89b4-3f40a11cc2b2	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Classic Cheeseburger	Beef patty with cheddar and pickles	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990230407	f	'and':8C 'beef':4C 'cheddar':7C 'cheeseburger':2A 'classic':1A 'italian':3B 'patty':5C 'pickles':9C 'with':6C	'cheeseburger':2 'classic':1	'and':5 'beef':1 'cheddar':4 'patty':2 'pickles':6 'with':3
ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	restaurant	\N	Bella Vista Italian	Authentic Italian cuisine in the heart of downtown	Italian	\N	\N	t	t	0	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.213939+00	2025-10-11 11:13:07.213939+00	638957779988496534	f	'authentic':5C 'bella':1A 'cuisine':7C 'downtown':12C 'heart':10C 'in':8C 'italian':3A,4B,6C 'of':11C 'the':9C 'vista':2A	'bella':1 'italian':3 'vista':2	'authentic':1 'cuisine':3 'downtown':8 'heart':6 'in':4 'italian':2 'of':7 'the':5
3ecaa164-e3fe-4d0b-b80e-20d990e32300	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Espresso	Double shot	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989082686	f	'double':3C 'espresso':1A 'japanese':2B 'shot':4C	'espresso':1	'double':1 'shot':2
88e4094f-4575-4654-9f20-2ae176f8eb8c	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Coffee	Freshly brewed	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989094618	f	'brewed':4C 'coffee':1A 'freshly':3C 'japanese':2B	'coffee':1	'brewed':2 'freshly':1
9bcea685-a5f5-40a5-ba5f-ac90aa65605c	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Brownie Sundae	Fudge brownie, ice cream, nuts	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989537476	f	'brownie':1A,5C 'cream':7C 'fudge':4C 'ice':6C 'mexican':3B 'nuts':8C 'sundae':2A	'brownie':1 'sundae':2	'brownie':2 'cream':4 'fudge':1 'ice':3 'nuts':5
a6b616f9-4017-416a-89d0-e6c4ba9b7e6e	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989542839	f	'fry':3A 'ginger':10C 'glaze':11C 'mexican':4B 'mixed':5C 'soy':9C 'soy-ginger':8C 'stir':2A 'vegetables':6C 'veggie':1A 'with':7C	'fry':3 'stir':2 'veggie':1	'ginger':6 'glaze':7 'mixed':1 'soy':5 'soy-ginger':4 'vegetables':2 'with':3
52beb617-39b9-4f83-b633-dc5693834c95	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Tiramisu	Espresso-soaked ladyfingers, mascarpone	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990230409	f	'espresso':4C 'espresso-soaked':3C 'italian':2B 'ladyfingers':6C 'mascarpone':7C 'soaked':5C 'tiramisu':1A	'tiramisu':1	'espresso':2 'espresso-soaked':1 'ladyfingers':4 'mascarpone':5 'soaked':3
cdaf5d43-ec68-4221-abcc-26ffb02ab654	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Cheesecake	Classic New York style	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988784692	f	'cheesecake':1A 'classic':3C 'japanese':2B 'new':4C 'style':6C 'york':5C	'cheesecake':1	'classic':1 'new':2 'style':4 'york':3
fb6089de-84bb-499f-b780-366129edf21d	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Espresso	Double shot	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989543049	f	'double':3C 'espresso':1A 'mexican':2B 'shot':4C	'espresso':1	'double':1 'shot':2
ee82f106-49d5-4df3-b5f2-a1c3879b0ac6	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990199623	f	'bbq':1A,5C 'chicken':2A,7C 'italian':4B 'onion':9C 'pizza':3A 'red':8C 'sauce':6C	'bbq':1 'chicken':2 'pizza':3	'bbq':1 'chicken':3 'onion':5 'red':4 'sauce':2
cee61414-3d95-496b-be96-5c614a2a2fb9	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Margherita Pizza	Tomato, mozzarella, basil	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990221065	f	'basil':6C 'italian':3B 'margherita':1A 'mozzarella':5C 'pizza':2A 'tomato':4C	'margherita':1 'pizza':2	'basil':3 'mozzarella':2 'tomato':1
d31dc458-2b6d-4f36-a3c3-fdcc400cc732	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Grilled Salmon	Lemon herb butter, seasonal veggies	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989561171	f	'butter':6C 'grilled':1A 'herb':5C 'lemon':4C 'mexican':3B 'salmon':2A 'seasonal':7C 'veggies':8C	'grilled':1 'salmon':2	'butter':3 'herb':2 'lemon':1 'seasonal':4 'veggies':5
ef7cdc35-2bea-49db-a6ae-6ac16e76cf78	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Gelato Trio	Three seasonal flavors	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989889592	f	'flavors':6C 'gelato':1A 'mexican':3B 'seasonal':5C 'three':4C 'trio':2A	'gelato':1 'trio':2	'flavors':3 'seasonal':2 'three':1
dfed149b-69b5-489b-bf64-df97d42e9fcc	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Iced Tea	Unsweetened black tea	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989006785	f	'black':5C 'iced':1A 'japanese':3B 'tea':2A,6C 'unsweetened':4C	'iced':1 'tea':2	'black':2 'tea':3 'unsweetened':1
30aedc59-7126-47bf-9e91-8d1d7b73abbd	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Brownie Sundae	Fudge brownie, ice cream, nuts	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990058426	f	'brownie':1A,5C 'cream':7C 'fudge':4C 'ice':6C 'italian':3B 'nuts':8C 'sundae':2A	'brownie':1 'sundae':2	'brownie':2 'cream':4 'fudge':1 'ice':3 'nuts':5
c88f9fca-c7de-4bdd-a6a4-ae1d563f3f4c	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Bruschetta	Grilled bread with tomato, basil, and garlic	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990078964	f	'and':8C 'basil':7C 'bread':4C 'bruschetta':1A 'garlic':9C 'grilled':3C 'italian':2B 'tomato':6C 'with':5C	'bruschetta':1	'and':6 'basil':5 'bread':2 'garlic':7 'grilled':1 'tomato':4 'with':3
a024ef34-2032-4692-b7f2-9fdaea3dfa67	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Caprese Skewers	Tomato, mozzarella, basil drizzle	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990103070	f	'basil':6C 'caprese':1A 'drizzle':7C 'italian':3B 'mozzarella':5C 'skewers':2A 'tomato':4C	'caprese':1 'skewers':2	'basil':3 'drizzle':4 'mozzarella':2 'tomato':1
b1214309-a149-43e9-8140-d678c5989d03	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Iced Tea	Unsweetened black tea	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990090945	f	'black':5C 'iced':1A 'italian':3B 'tea':2A,6C 'unsweetened':4C	'iced':1 'tea':2	'black':2 'tea':3 'unsweetened':1
998a2068-9c33-440b-8703-91e8ae26ab55	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Fried Calamari	Lightly breaded rings with marinara	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990133535	f	'breaded':5C 'calamari':2A 'fried':1A 'italian':3B 'lightly':4C 'marinara':8C 'rings':6C 'with':7C	'calamari':2 'fried':1	'breaded':2 'lightly':1 'marinara':5 'rings':3 'with':4
92a026ff-8a3d-40c4-819c-1612d27de491	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Cola	Classic soda	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990164477	f	'classic':3C 'cola':1A 'italian':2B 'soda':4C	'cola':1	'classic':1 'soda':2
8f060a14-13b5-4975-b53f-a1b94323a6cd	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Apple Pie	Cinnamon crumb topping	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990172673	f	'apple':1A 'cinnamon':4C 'crumb':5C 'italian':3B 'pie':2A 'topping':6C	'apple':1 'pie':2	'cinnamon':1 'crumb':2 'topping':3
803cadb4-778b-406c-927e-9c25990323be	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Spinach Artichoke Dip	Creamy dip with tortilla chips	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990172671	f	'artichoke':2A 'chips':9C 'creamy':5C 'dip':3A,6C 'italian':4B 'spinach':1A 'tortilla':8C 'with':7C	'artichoke':2 'dip':3 'spinach':1	'chips':5 'creamy':1 'dip':2 'tortilla':4 'with':3
d2a1653e-2dc4-4c0d-822f-406e1485626a	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	House Lemonade	Fresh squeezed	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990341100	f	'fresh':4C 'house':1A 'italian':3B 'lemonade':2A 'squeezed':5C	'house':1 'lemonade':2	'fresh':1 'squeezed':2
6cd0b99c-5e44-45f2-901a-f2ecfd1e9c55	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Espresso	Double shot	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990349627	f	'double':3C 'espresso':1A 'italian':2B 'shot':4C	'espresso':1	'double':1 'shot':2
4295aed6-65c3-4868-805b-8eea625ab2d3	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Coffee	Freshly brewed	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990364132	f	'brewed':4C 'coffee':1A 'freshly':3C 'italian':2B	'coffee':1	'brewed':2 'freshly':1
42816823-c188-4bb7-8800-d78c475dc468	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Stuffed Mushrooms	Herb cream cheese, baked	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990388108	f	'baked':7C 'cheese':6C 'cream':5C 'herb':4C 'italian':3B 'mushrooms':2A 'stuffed':1A	'mushrooms':2 'stuffed':1	'baked':4 'cheese':3 'cream':2 'herb':1
3dcf1522-5b98-450a-817b-8cf99d732da7	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Grilled Salmon	Lemon herb butter, seasonal veggies	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990401912	f	'butter':6C 'grilled':1A 'herb':5C 'italian':3B 'lemon':4C 'salmon':2A 'seasonal':7C 'veggies':8C	'grilled':1 'salmon':2	'butter':3 'herb':2 'lemon':1 'seasonal':4 'veggies':5
f5514f09-9109-4906-8d27-e76bd0269e4e	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	House Lemonade	Fresh squeezed	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989006730	f	'fresh':4C 'house':1A 'japanese':3B 'lemonade':2A 'squeezed':5C	'house':1 'lemonade':2	'fresh':1 'squeezed':2
d5037afa-ede0-4f3a-b8b0-9e8ff6fa34e8	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Classic Cheeseburger	Beef patty with cheddar and pickles	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989060787	f	'and':8C 'beef':4C 'cheddar':7C 'cheeseburger':2A 'classic':1A 'japanese':3B 'patty':5C 'pickles':9C 'with':6C	'cheeseburger':2 'classic':1	'and':5 'beef':1 'cheddar':4 'patty':2 'pickles':6 'with':3
d5c9f786-d7cf-4ee7-926c-5a97f0d4879a	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Chocolate Lava Cake	Warm center, vanilla ice cream	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988996036	f	'cake':3A 'center':6C 'chocolate':1A 'cream':9C 'ice':8C 'japanese':4B 'lava':2A 'vanilla':7C 'warm':5C	'cake':3 'chocolate':1 'lava':2	'center':2 'cream':5 'ice':4 'vanilla':3 'warm':1
fa2bae82-ffb8-41dd-90ce-f950cff7d1e2	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990394325	f	'fry':3A 'ginger':10C 'glaze':11C 'italian':4B 'mixed':5C 'soy':9C 'soy-ginger':8C 'stir':2A 'vegetables':6C 'veggie':1A 'with':7C	'fry':3 'stir':2 'veggie':1	'ginger':6 'glaze':7 'mixed':1 'soy':5 'soy-ginger':4 'vegetables':2 'with':3
e5b17d35-247b-4163-8ee3-6481005e4c39	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Stuffed Mushrooms	Herb cream cheese, baked	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988972705	f	'baked':7C 'cheese':6C 'cream':5C 'herb':4C 'japanese':3B 'mushrooms':2A 'stuffed':1A	'mushrooms':2 'stuffed':1	'baked':4 'cheese':3 'cream':2 'herb':1
1f82bf13-e864-4b4e-a6d3-40f413d7a3fa	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Grilled Salmon	Lemon herb butter, seasonal veggies	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988702902	f	'butter':6C 'grilled':1A 'herb':5C 'japanese':3B 'lemon':4C 'salmon':2A 'seasonal':7C 'veggies':8C	'grilled':1 'salmon':2	'butter':3 'herb':2 'lemon':1 'seasonal':4 'veggies':5
047dc237-1270-41e5-bc65-d2d8dd032f59	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Tiramisu	Espresso-soaked ladyfingers, mascarpone	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988702951	f	'espresso':4C 'espresso-soaked':3C 'japanese':2B 'ladyfingers':6C 'mascarpone':7C 'soaked':5C 'tiramisu':1A	'tiramisu':1	'espresso':2 'espresso-soaked':1 'ladyfingers':4 'mascarpone':5 'soaked':3
74680349-bc5b-4b5f-a8cc-6e24f8858060	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Fried Calamari	Lightly breaded rings with marinara	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988774800	f	'breaded':5C 'calamari':2A 'fried':1A 'japanese':3B 'lightly':4C 'marinara':8C 'rings':6C 'with':7C	'calamari':2 'fried':1	'breaded':2 'lightly':1 'marinara':5 'rings':3 'with':4
cd08bf0e-b435-4100-9426-73a4822195ac	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Spinach Artichoke Dip	Creamy dip with tortilla chips	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988849751	f	'artichoke':2A 'chips':9C 'creamy':5C 'dip':3A,6C 'japanese':4B 'spinach':1A 'tortilla':8C 'with':7C	'artichoke':2 'dip':3 'spinach':1	'chips':5 'creamy':1 'dip':2 'tortilla':4 'with':3
b8653a52-7863-43a1-9c69-717dab9592bc	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Sparkling Water	Chilled with lemon	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988855385	f	'chilled':4C 'japanese':3B 'lemon':6C 'sparkling':1A 'water':2A 'with':5C	'sparkling':1 'water':2	'chilled':1 'lemon':3 'with':2
b537baf9-6b33-4e7f-809c-623d54c673ef	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Apple Pie	Cinnamon crumb topping	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988855421	f	'apple':1A 'cinnamon':4C 'crumb':5C 'japanese':3B 'pie':2A 'topping':6C	'apple':1 'pie':2	'cinnamon':1 'crumb':2 'topping':3
58547858-f9ec-4168-951d-69097bd4fae7	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Bruschetta	Grilled bread with tomato, basil, and garlic	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988703019	f	'and':8C 'basil':7C 'bread':4C 'bruschetta':1A 'garlic':9C 'grilled':3C 'japanese':2B 'tomato':6C 'with':5C	'bruschetta':1	'and':6 'basil':5 'bread':2 'garlic':7 'grilled':1 'tomato':4 'with':3
a54ecbbc-4cfc-439a-9835-f2bbfdbdcff1	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Veggie Stir Fry	Mixed vegetables with soy-ginger glaze	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988923051	f	'fry':3A 'ginger':10C 'glaze':11C 'japanese':4B 'mixed':5C 'soy':9C 'soy-ginger':8C 'stir':2A 'vegetables':6C 'veggie':1A 'with':7C	'fry':3 'stir':2 'veggie':1	'ginger':6 'glaze':7 'mixed':1 'soy':5 'soy-ginger':4 'vegetables':2 'with':3
b3a67239-0aed-4eeb-b575-8667462bcd2f	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988914361	f	'crispy':5C 'garlic':1A,9C 'in':8C 'japanese':4B 'parmesan':2A,10C 'tossed':7C 'wings':3A,6C	'garlic':1 'parmesan':2 'wings':3	'crispy':1 'garlic':5 'in':4 'parmesan':6 'tossed':3 'wings':2
ab7e62f7-385d-491f-995b-4eef9e80d920	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Gelato Trio	Three seasonal flavors	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988923052	f	'flavors':6C 'gelato':1A 'japanese':3B 'seasonal':5C 'three':4C 'trio':2A	'gelato':1 'trio':2	'flavors':3 'seasonal':2 'three':1
5ca19a50-b650-433e-9fec-5120e6f32474	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Margherita Pizza	Tomato, mozzarella, basil	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989070008	f	'basil':6C 'japanese':3B 'margherita':1A 'mozzarella':5C 'pizza':2A 'tomato':4C	'margherita':1 'pizza':2	'basil':3 'mozzarella':2 'tomato':1
3b99344d-be66-408c-857f-6c4f74b1997b	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989128563	f	'alfredo':2A 'chicken':1A 'creamy':5C 'fettuccine':8C 'japanese':4B 'parmesan':6C 'pasta':3A 'sauce':7C	'alfredo':2 'chicken':1 'pasta':3	'creamy':1 'fettuccine':4 'parmesan':2 'sauce':3
e846b7f3-5bea-49ee-a073-d39b2387fbc3	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Chocolate Lava Cake	Warm center, vanilla ice cream	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989898453	f	'cake':3A 'center':6C 'chocolate':1A 'cream':9C 'ice':8C 'lava':2A 'mexican':4B 'vanilla':7C 'warm':5C	'cake':3 'chocolate':1 'lava':2	'center':2 'cream':5 'ice':4 'vanilla':3 'warm':1
f262c9a1-cf0a-4fb5-8a9e-9a591fba0347	menu_item	ddf42b3d-dedc-4df0-98ea-bf1fa717cd88	Sparkling Water	Chilled with lemon	Italian	\N	\N	t	t	\N	0	\N	0101000020E61000004182E2C7988F5DC0F46C567DAE064140	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990283457	f	'chilled':4C 'italian':3B 'lemon':6C 'sparkling':1A 'water':2A 'with':5C	'sparkling':1 'water':2	'chilled':1 'lemon':3 'with':2
eb15247f-a1e3-44d4-b086-e68fb329ad83	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Brownie Sundae	Fudge brownie, ice cream, nuts	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988702889	f	'brownie':1A,5C 'cream':7C 'fudge':4C 'ice':6C 'japanese':3B 'nuts':8C 'sundae':2A	'brownie':1 'sundae':2	'brownie':2 'cream':4 'fudge':1 'ice':3 'nuts':5
10505f7f-4bb3-45b4-b5ee-9d9507a78854	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	Cola	Classic soda	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779988783085	f	'classic':3C 'cola':1A 'japanese':2B 'soda':4C	'cola':1	'classic':1 'soda':2
ff993a63-2a6d-41a9-864d-d122fcbc7e4f	menu_item	5eea6b99-6e3e-49da-a4ed-a94c96d7fd6a	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	Japanese	\N	\N	t	t	\N	0	\N	0101000020E61000001AC05B2041955EC0E86A2BF697CD4740	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989152423	f	'bbq':1A,5C 'chicken':2A,7C 'japanese':4B 'onion':9C 'pizza':3A 'red':8C 'sauce':6C	'bbq':1 'chicken':2 'pizza':3	'bbq':1 'chicken':3 'onion':5 'red':4 'sauce':2
6e804fcb-30a9-41ba-a1d8-571cb12aa302	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Bruschetta	Grilled bread with tomato, basil, and garlic	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989160799	f	'and':8C 'basil':7C 'bread':4C 'bruschetta':1A 'garlic':9C 'grilled':3C 'mexican':2B 'tomato':6C 'with':5C	'bruschetta':1	'and':6 'basil':5 'bread':2 'garlic':7 'grilled':1 'tomato':4 'with':3
bb14d2fe-861f-4761-b987-3b06a3d51b4f	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	BBQ Chicken Pizza	BBQ sauce, chicken, red onion	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989306023	f	'bbq':1A,5C 'chicken':2A,7C 'mexican':4B 'onion':9C 'pizza':3A 'red':8C 'sauce':6C	'bbq':1 'chicken':2 'pizza':3	'bbq':1 'chicken':3 'onion':5 'red':4 'sauce':2
38cc668e-53f5-452e-b778-d12c9123602f	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Margherita Pizza	Tomato, mozzarella, basil	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989922782	f	'basil':6C 'margherita':1A 'mexican':3B 'mozzarella':5C 'pizza':2A 'tomato':4C	'margherita':1 'pizza':2	'basil':3 'mozzarella':2 'tomato':1
3a4951fd-7591-4568-8053-ff98dce59f80	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Coffee	Freshly brewed	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989932229	f	'brewed':4C 'coffee':1A 'freshly':3C 'mexican':2B	'coffee':1	'brewed':2 'freshly':1
4a36de13-59f1-47fc-9809-2e60c45c5f05	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Chicken Alfredo Pasta	Creamy parmesan sauce, fettuccine	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989957911	f	'alfredo':2A 'chicken':1A 'creamy':5C 'fettuccine':8C 'mexican':4B 'parmesan':6C 'pasta':3A 'sauce':7C	'alfredo':2 'chicken':1 'pasta':3	'creamy':1 'fettuccine':4 'parmesan':2 'sauce':3
598398ba-e8ce-4627-9fb1-b71561a0c63c	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Garlic Parmesan Wings	Crispy wings tossed in garlic parmesan	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779989968419	f	'crispy':5C 'garlic':1A,9C 'in':8C 'mexican':4B 'parmesan':2A,10C 'tossed':7C 'wings':3A,6C	'garlic':1 'parmesan':2 'wings':3	'crispy':1 'garlic':5 'in':4 'parmesan':6 'tossed':3 'wings':2
7006f065-3305-4722-9671-c575c939c6d9	menu_item	7019b300-9f2a-4758-9745-dd71fb74c327	Caprese Skewers	Tomato, mozzarella, basil drizzle	Mexican	\N	\N	t	t	\N	0	\N	0101000020E6100000166A4DF38E6F58C0BF7D1D3867443E40	2025-10-11 11:13:07.802033+00	2025-10-11 11:13:07.802033+00	638957779990017417	f	'basil':6C 'caprese':1A 'drizzle':7C 'mexican':3B 'mozzarella':5C 'skewers':2A 'tomato':4C	'caprese':1 'skewers':2	'basil':3 'drizzle':4 'mozzarella':2 'tomato':1
\.


--
-- Data for Name: SupportTickets; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SupportTickets" ("Id", "TicketNumber", "Subject", "Status", "Priority", "Type", "SubmissionTimestamp", "LastUpdateTimestamp", "AssignedToAdminId", "Created", "CreatedBy") FROM stdin;
\.


--
-- Data for Name: SupportTicketContextLinks; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SupportTicketContextLinks" ("EntityType", "EntityID", "SupportTicketId") FROM stdin;
\.


--
-- Data for Name: SupportTicketMessages; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."SupportTicketMessages" ("MessageId", "SupportTicketId", "AuthorId", "AuthorType", "MessageText", "Timestamp", "IsInternalNote") FROM stdin;
\.


--
-- Data for Name: Tags; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."Tags" ("Id", "TagName", "TagDescription", "TagCategory", "Created", "CreatedBy", "LastModified", "LastModifiedBy", "IsDeleted", "DeletedOn", "DeletedBy") FROM stdin;
1042cb47-0ad2-4c8b-b1b6-541eac282b2e	Italian	\N	Cuisine	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
10e5d13d-b9b6-4094-a1d6-d9f507f94397	Vegan	No animal products	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
388ee244-2de3-49e2-9a2c-565f31d5d356	Hot	\N	SpiceLevel	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
4c44bb12-24e1-47dc-a41d-e7b0cb96c9ca	Dairy-Free	\N	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
7bb10fce-b39c-4b60-b090-53c928a9281a	Mexican	\N	Cuisine	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
8ba88a50-9c62-435f-b934-c0b278610e01	Mild	\N	SpiceLevel	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
ac707753-52f0-49a7-bf5d-97699986a0fa	Vegetarian	No meat or fish	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
c65b0cf0-a761-4e0d-8aa5-ea68fbdba3bf	Gluten-Free	No gluten ingredients	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
ccbc5525-043b-4e6f-bee7-2b95a7a78726	Halal	\N	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
eca6d281-aa3e-4a6c-83e4-7379b4981c4a	Kosher	\N	Dietary	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
f6f0e99f-4e0b-4a5c-b149-8c1444a82142	Japanese	\N	Cuisine	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
f7ce9912-de60-4890-a2f4-c04bcd218163	Medium	\N	SpiceLevel	2025-10-11 11:13:07.298839+00	\N	2025-10-11 11:13:07.298839+00	\N	f	\N	\N
\.


--
-- Data for Name: TeamCarts; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamCarts" ("Id", "Created", "CreatedBy", "RestaurantId", "HostUserId", "Status", "ShareToken_Value", "ShareToken_ExpiresAt", "Deadline", "CreatedAt", "ExpiresAt", "TipAmount_Amount", "TipAmount_Currency", "AppliedCouponId", "GrandTotal_Amount", "GrandTotal_Currency", "MemberTotals", "QuoteVersion") FROM stdin;
\.


--
-- Data for Name: TeamCartItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamCartItems" ("TeamCartItemId", "TeamCartId", "AddedByUserId", "Snapshot_MenuItemId", "Snapshot_MenuCategoryId", "Snapshot_ItemName", "BasePrice_Amount", "BasePrice_Currency", "Quantity", "LineItemTotal_Amount", "LineItemTotal_Currency", "SelectedCustomizations") FROM stdin;
\.


--
-- Data for Name: TeamCartMemberPayments; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamCartMemberPayments" ("MemberPaymentId", "TeamCartId", "UserId", "Payment_Amount", "Payment_Currency", "Method", "Status", "OnlineTransactionId", "CreatedAt", "UpdatedAt") FROM stdin;
\.


--
-- Data for Name: TeamCartMembers; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TeamCartMembers" ("TeamCartMemberId", "TeamCartId", "UserId", "Name", "Role") FROM stdin;
\.


--
-- Data for Name: TodoLists; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TodoLists" ("Id", "Title", "Colour", "Created", "CreatedBy", "LastModified", "LastModifiedBy") FROM stdin;
\.


--
-- Data for Name: TodoItems; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."TodoItems" ("TodoItemId", "TodoListId", "Title", "Note", "Priority", "Reminder", "IsDone", "Created", "CreatedBy", "LastModified", "LastModifiedBy") FROM stdin;
\.


--
-- Data for Name: UserAddresses; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UserAddresses" ("AddressId", "UserId", "Street", "City", "State", "ZipCode", "Country", "Label", "DeliveryInstructions") FROM stdin;
\.


--
-- Data for Name: UserDeviceSessions; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UserDeviceSessions" ("Id", "UserId", "DeviceId", "FcmToken", "IsActive", "LastLoginAt", "LoggedOutAt") FROM stdin;
571d37b6-d46f-442c-b642-9fc4155a6548	0199d2f9-7123-7cbc-9983-257346f68c52	f248a6cf-10e7-4d3c-bea5-7ca58802ab1b	eaVgeLKNTgqOSMFsNmUYo5:APA91bEmTbtxCFo_888pNUNEUi6euk66GP6iYbtWV_Sq2uWeb81IPO1LMfKpsabH77N_xjdVwmNgd3ms3xTLf92iK8DVvr-Zh_bOKB-wZIm3Ns3E5T0O5Xs	t	2025-10-11 11:13:06.992464+00	\N
df965d88-dd34-4a0a-8ded-9ef541061607	0199d2f9-7213-7896-8b7c-490984904041	9972124f-4634-45a1-ad90-ae0aa57de178	cjYujyIsRHGCBNHQaIsvy2:APA91bErEQrSMTKusz8AkdpswagbOwt4x2FqkTJRURMa6xg1HpcDANoxEH0RTz-J4hlC1QXOIKsjBtwBk6pn8JVWNLliQxRYtYHHVrI77doOwPOrLxHQruE	t	2025-10-11 11:13:07.127248+00	\N
\.


--
-- Data for Name: UserPaymentMethods; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."UserPaymentMethods" ("PaymentMethodId", "UserId", "Type", "TokenizedDetails", "IsDefault", "Created", "CreatedBy", "LastModified", "LastModifiedBy") FROM stdin;
\.


--
-- Data for Name: __EFMigrationsHistory; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public."__EFMigrationsHistory" ("MigrationId", "ProductVersion") FROM stdin;
20250818163206_InitialMigration	9.0.6
20250903163813_AddSearchIndex	9.0.6
20250912042826_AddTeamCartQuoteLite	9.0.6
20250916081328_UpdateUsers	9.0.6
20250917124008_AddRestaurantRegistrations	9.0.6
20250917173525_ExtendRestaurantReviewSummary	9.0.6
20250918044926_AddUniqueActiveReviewIndex	9.0.6
20250918131013_FixCouponUsageLimitPerUserNullable	9.0.6
20250919083605_AddAdminMetricsReadModels	9.0.6
\.


--
-- Data for Name: spatial_ref_sys; Type: TABLE DATA; Schema: public; Owner: -
--

COPY public.spatial_ref_sys (srid, auth_name, auth_srid, srtext, proj4text) FROM stdin;
\.


--
-- Data for Name: geocode_settings; Type: TABLE DATA; Schema: tiger; Owner: -
--

COPY tiger.geocode_settings (name, setting, unit, category, short_desc) FROM stdin;
\.


--
-- Data for Name: pagc_gaz; Type: TABLE DATA; Schema: tiger; Owner: -
--

COPY tiger.pagc_gaz (id, seq, word, stdword, token, is_custom) FROM stdin;
\.


--
-- Data for Name: pagc_lex; Type: TABLE DATA; Schema: tiger; Owner: -
--

COPY tiger.pagc_lex (id, seq, word, stdword, token, is_custom) FROM stdin;
\.


--
-- Data for Name: pagc_rules; Type: TABLE DATA; Schema: tiger; Owner: -
--

COPY tiger.pagc_rules (id, rule, is_custom) FROM stdin;
\.


--
-- Data for Name: topology; Type: TABLE DATA; Schema: topology; Owner: -
--

COPY topology.topology (id, name, srid, "precision", hasz) FROM stdin;
\.


--
-- Data for Name: layer; Type: TABLE DATA; Schema: topology; Owner: -
--

COPY topology.layer (topology_id, layer_id, schema_name, table_name, feature_column, feature_type, level, child_id) FROM stdin;
\.


--
-- Name: AspNetRoleClaims_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."AspNetRoleClaims_Id_seq"', 1, false);


--
-- Name: AspNetUserClaims_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: -
--

SELECT pg_catalog.setval('public."AspNetUserClaims_Id_seq"', 1, false);


--
-- Name: topology_id_seq; Type: SEQUENCE SET; Schema: topology; Owner: -
--

SELECT pg_catalog.setval('topology.topology_id_seq', 1, false);


--
-- PostgreSQL database dump complete
--

