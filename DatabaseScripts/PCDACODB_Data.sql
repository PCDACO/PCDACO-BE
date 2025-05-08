--
-- PostgreSQL database dump
--

-- Dumped from database version 13.5 (Debian 13.5-1.pgdg110+1)
-- Dumped by pg_dump version 13.5 (Debian 13.5-1.pgdg110+1)

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
-- Data for Name: aggregatedcounter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: counter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: hash; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

INSERT INTO hangfire.hash VALUES (1, 'recurring-job:update-car-statistics', 'Queue', 'default', NULL, 0);
INSERT INTO hangfire.hash VALUES (2, 'recurring-job:update-car-statistics', 'Cron', '0 * * * *', NULL, 0);
INSERT INTO hangfire.hash VALUES (3, 'recurring-job:update-car-statistics', 'TimeZoneId', 'UTC', NULL, 0);
INSERT INTO hangfire.hash VALUES (4, 'recurring-job:update-car-statistics', 'Job', '{"Type":"UseCases.BackgroundServices.Statistics.UpdateCarStatisticsJob, UseCases, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","Method":"UpdateCarStatistic","ParameterTypes":"[]","Arguments":"[]"}', NULL, 0);
INSERT INTO hangfire.hash VALUES (5, 'recurring-job:update-car-statistics', 'CreatedAt', '2025-04-29T06:40:44.5155953Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (6, 'recurring-job:update-car-statistics', 'NextExecution', '2025-04-29T07:00:00.0000000Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (7, 'recurring-job:update-car-statistics', 'V', '2', NULL, 0);
INSERT INTO hangfire.hash VALUES (8, 'recurring-job:update-user-statistics', 'Queue', 'default', NULL, 0);
INSERT INTO hangfire.hash VALUES (9, 'recurring-job:update-user-statistics', 'Cron', '0 * * * *', NULL, 0);
INSERT INTO hangfire.hash VALUES (10, 'recurring-job:update-user-statistics', 'TimeZoneId', 'UTC', NULL, 0);
INSERT INTO hangfire.hash VALUES (11, 'recurring-job:update-user-statistics', 'Job', '{"Type":"UseCases.BackgroundServices.Statistics.UpdateUserStatisticsJob, UseCases, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","Method":"UpdateUserStatistic","ParameterTypes":"[]","Arguments":"[]"}', NULL, 0);
INSERT INTO hangfire.hash VALUES (12, 'recurring-job:update-user-statistics', 'CreatedAt', '2025-04-29T06:40:44.6208655Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (13, 'recurring-job:update-user-statistics', 'NextExecution', '2025-04-29T07:00:00.0000000Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (14, 'recurring-job:update-user-statistics', 'V', '2', NULL, 0);
INSERT INTO hangfire.hash VALUES (15, 'recurring-job:expire-bookings-automatically', 'Queue', 'default', NULL, 0);
INSERT INTO hangfire.hash VALUES (16, 'recurring-job:expire-bookings-automatically', 'Cron', '*/15 * * * *', NULL, 0);
INSERT INTO hangfire.hash VALUES (17, 'recurring-job:expire-bookings-automatically', 'TimeZoneId', 'UTC', NULL, 0);
INSERT INTO hangfire.hash VALUES (18, 'recurring-job:expire-bookings-automatically', 'Job', '{"Type":"UseCases.BackgroundServices.Bookings.BookingExpiredJob, UseCases, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","Method":"ExpireBookingsAutomatically","ParameterTypes":"[]","Arguments":"[]"}', NULL, 0);
INSERT INTO hangfire.hash VALUES (19, 'recurring-job:expire-bookings-automatically', 'CreatedAt', '2025-04-29T06:40:44.6376674Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (20, 'recurring-job:expire-bookings-automatically', 'NextExecution', '2025-04-29T06:45:00.0000000Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (21, 'recurring-job:expire-bookings-automatically', 'V', '2', NULL, 0);
INSERT INTO hangfire.hash VALUES (22, 'recurring-job:handle-overdue-bookings', 'Queue', 'default', NULL, 0);
INSERT INTO hangfire.hash VALUES (23, 'recurring-job:handle-overdue-bookings', 'Cron', '*/15 * * * *', NULL, 0);
INSERT INTO hangfire.hash VALUES (24, 'recurring-job:handle-overdue-bookings', 'TimeZoneId', 'UTC', NULL, 0);
INSERT INTO hangfire.hash VALUES (25, 'recurring-job:handle-overdue-bookings', 'Job', '{"Type":"UseCases.BackgroundServices.Bookings.BookingOverdueJob, UseCases, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","Method":"HandleOverdueBookings","ParameterTypes":"[]","Arguments":"[]"}', NULL, 0);
INSERT INTO hangfire.hash VALUES (26, 'recurring-job:handle-overdue-bookings', 'CreatedAt', '2025-04-29T06:40:44.6510533Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (27, 'recurring-job:handle-overdue-bookings', 'NextExecution', '2025-04-29T06:45:00.0000000Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (28, 'recurring-job:handle-overdue-bookings', 'V', '2', NULL, 0);
INSERT INTO hangfire.hash VALUES (29, 'recurring-job:expire-inspection-schedules-automatically', 'Queue', 'default', NULL, 0);
INSERT INTO hangfire.hash VALUES (30, 'recurring-job:expire-inspection-schedules-automatically', 'Cron', '* * * * *', NULL, 0);
INSERT INTO hangfire.hash VALUES (31, 'recurring-job:expire-inspection-schedules-automatically', 'TimeZoneId', 'UTC', NULL, 0);
INSERT INTO hangfire.hash VALUES (32, 'recurring-job:expire-inspection-schedules-automatically', 'Job', '{"Type":"UseCases.BackgroundServices.InspectionSchedule.InspectionScheduleExpiredJob, UseCases, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null","Method":"ExpireInspectionSchedulesAutomatically","ParameterTypes":"[]","Arguments":"[]"}', NULL, 0);
INSERT INTO hangfire.hash VALUES (33, 'recurring-job:expire-inspection-schedules-automatically', 'CreatedAt', '2025-04-29T06:40:44.6648336Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (34, 'recurring-job:expire-inspection-schedules-automatically', 'NextExecution', '2025-04-29T06:41:00.0000000Z', NULL, 0);
INSERT INTO hangfire.hash VALUES (35, 'recurring-job:expire-inspection-schedules-automatically', 'V', '2', NULL, 0);


--
-- Data for Name: job; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: jobparameter; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: jobqueue; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: list; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: lock; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: schema; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

INSERT INTO hangfire.schema VALUES (23);


--
-- Data for Name: server; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

INSERT INTO hangfire.server VALUES ('mglongbao:15032:83783dd8-b93b-47ca-a8d3-1e9185ee0665', '{"Queues": ["default"], "StartedAt": "2025-04-29T06:40:45.2633381Z", "WorkerCount": 20}', '2025-04-29 06:40:45.270995+00', 0);


--
-- Data for Name: set; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--

INSERT INTO hangfire.set VALUES (1, 'recurring-jobs', 1745910000, 'update-car-statistics', NULL, 0);
INSERT INTO hangfire.set VALUES (2, 'recurring-jobs', 1745910000, 'update-user-statistics', NULL, 0);
INSERT INTO hangfire.set VALUES (3, 'recurring-jobs', 1745909100, 'expire-bookings-automatically', NULL, 0);
INSERT INTO hangfire.set VALUES (4, 'recurring-jobs', 1745909100, 'handle-overdue-bookings', NULL, 0);
INSERT INTO hangfire.set VALUES (5, 'recurring-jobs', 1745908860, 'expire-inspection-schedules-automatically', NULL, 0);


--
-- Data for Name: state; Type: TABLE DATA; Schema: hangfire; Owner: postgres
--



--
-- Data for Name: Amenities; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Amenities" VALUES ('01968046-c448-73ef-87bc-80a8c55a79ce', 'Bản Đồ', 'Hệ thống bản đồ giúp tài xế định hướng và tìm đường dễ dàng hơn khi di chuyển.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/evc9i1k2drng1a7lm7bt.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-71ff-8cac-455544411be5', 'Camera Hành Trình', 'Camera ghi lại hành trình di chuyển, giúp lưu lại bằng chứng khi xảy ra va chạm.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/xyyiipesinpmlr2al9ah.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7200-a0c8-0dfc5c2d1105', 'Cảnh Báo Tốc Độ', 'Cảnh báo khi xe vượt quá tốc độ giới hạn, giúp tài xế lái xe an toàn hơn.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764803/amenity-icon/s6cqhwmvarvjpopaj8ts.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7201-8d22-6c9c2ba0b2f6', 'Lốp Dự Phòng', 'Lốp dự phòng thay thế khi xe bị thủng lốp hoặc hư hỏng lốp trên đường.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/wxd8wdeihezfb44u2rza.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7202-9c33-79707542c0fe', 'Camera 360', 'Hệ thống camera toàn cảnh giúp tài xế quan sát xung quanh xe một cách trực quan.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/qrl8hs1lnw8s9im24iza.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7203-8c5d-485b10d4b598', 'Cảm Biến Lốp', 'Cảm biến theo dõi áp suất lốp, cảnh báo khi lốp bị non hơi hoặc quá căng.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/fw8zia6c92zdlmt4gwxu.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7204-948a-e6f0158e18b9', 'Định Vị GPS', 'Hệ thống định vị giúp xác định vị trí xe theo thời gian thực.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764804/amenity-icon/sj8tklczqfnn2tfskekq.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7205-9a06-d5172c1b330e', 'ETC', 'Hệ thống thu phí không dừng, giúp xe di chuyển qua trạm thu phí mà không cần dừng lại.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764804/amenity-icon/lxuziipunkd0ejk7m1mq.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7206-808f-ed59504518ea', 'Bluetooth', 'Kết nối không dây giữa điện thoại và xe để nghe nhạc, nhận cuộc gọi rảnh tay.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/pmt7a8hlbny2yy1ydkof.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7207-8294-4931a544e613', 'Camera Lùi', 'Hỗ trợ quan sát phía sau khi lùi xe, giúp tránh va chạm.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/ti6manevdxmvybb8yrcs.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7208-b24c-7f709466b497', 'Cửa Sổ Trời', 'Cửa sổ trên nóc xe, giúp không gian xe thoáng đãng hơn.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764803/amenity-icon/vwqodbsvuauaj9ghz1ok.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-7209-aad2-ec8b5513fe66', 'Màn Hình DVD', 'Màn hình giải trí hiển thị video, bản đồ hoặc thông tin xe.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/n9swnzsyen6gwscs84gm.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-720a-bffd-7faa6133739e', 'Camera Cập Lề', 'Camera hỗ trợ đỗ xe, giúp tài xế quan sát khi cập lề đường.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/ixmpvqruaxw66popspzd.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-720b-82a4-01a19a5ffc51', 'Cảm Biến Va Chạm', 'Cảm biến cảnh báo khi xe sắp va chạm với vật cản phía trước hoặc phía sau.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/guxyshbopwmoktxzbgz0.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-720c-b63e-f98847ed5152', 'Khe Cắm USB', 'Cổng USB để sạc thiết bị hoặc kết nối với hệ thống giải trí trên xe.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764800/amenity-icon/civrmsfcb0kenvllo6hh.svg', NULL, NULL, false);
INSERT INTO public."Amenities" VALUES ('01968046-c44b-720d-960a-a6a983b1b10f', 'Túi Khí An Toàn', 'Túi khí giúp giảm chấn thương khi xảy ra va chạm.', 'https://res.cloudinary.com/ds2bfbfyd/image/upload/v1739764802/amenity-icon/fzkmgpy7ztsw5ptnyzlf.svg', NULL, NULL, false);


--
-- Data for Name: BankInfos; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."BankInfos" VALUES ('01968046-c44f-73ff-a2fe-04e21c76a2ab', '2d71f051-72fa-48bd-a362-27a08e8df3b9', 'Ngân hàng TMCP An Bình', 'ABB', 970425, 'ABBANK', 'https://api.vietqr.io/img/ABB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/ABB.svg', 'ABBKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7717-b14c-6772c34bcece', '5b8bb807-6d1c-485b-986e-ad0fb2a6d4d2', 'Ngân hàng TMCP Á Châu', 'ACB', 970416, 'ACB', 'https://api.vietqr.io/img/ACB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/ACB.svg', 'ASCBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7718-a311-c171cc8641b0', 'f599edf9-40c8-4bbf-87e8-230c1787a439', 'Ngân hàng TMCP Bắc Á', 'BAB', 970409, 'BacABank', 'https://api.vietqr.io/img/BAB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/BAB.svg', 'NASCVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7719-9f9f-6639e8e8dcf5', 'f956e517-d4c1-43b1-bbbb-277bdc5f9037', 'Ngân hàng TMCP Đầu tư và Phát triển Việt Nam', 'BIDV', 970418, 'BIDV', 'https://api.vietqr.io/img/BIDV.png', 'https://cdn.banklookup.net/assets/images/bank-icons/BIDV.svg', 'BIDVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771a-a011-64e5c32ac417', 'a4f2d7b2-7b85-4420-8273-3c5d0a69e8f1', 'Ngân hàng TMCP Bảo Việt', 'BVB', 970438, 'BaoVietBank', 'https://api.vietqr.io/img/BVB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/BVB.svg', 'BVBVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771b-9b92-e1750be93c30', '66fa0378-a568-4ca0-b958-deb03de55ab4', 'TMCP Việt Nam Thịnh Vượng - Ngân hàng số CAKE by VPBank', 'CAKE', 546034, 'CAKE', 'https://api.vietqr.io/img/CAKE.png', 'https://cdn.banklookup.net/assets/images/bank-icons/CAKE.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771c-a2e8-35e6ef7fdcb2', '78d48899-8cf5-48d7-8572-645f43b0880d', 'Ngân hàng Thương mại TNHH MTV Xây dựng Việt Nam', 'CBB', 970444, 'CBBank', 'https://api.vietqr.io/img/CBB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/CBB.svg', 'GTBAVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771d-85e9-ec81efaf519c', 'd4e933a0-e11e-470b-9634-30638e4dfa66', 'Ngân hàng TNHH MTV CIMB Việt Nam', 'CIMB', 422589, 'CIMB', 'https://api.vietqr.io/img/CIMB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/CIMB.svg', 'CIBBVNVN', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771e-97e5-add2a29bba8f', '1fe21f42-9b91-418c-a364-6e3190365009', 'Ngân hàng Hợp tác xã Việt Nam', 'COOPB', 970446, 'Co-op Bank', 'https://api.vietqr.io/img/COOPBANK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/COOPBANK.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-771f-a2e9-942421a3963b', 'dc6920cb-8d1f-4393-a01d-db53d4b0e289', 'Ngân hàng TMCP Đông Á', 'DAB', 970406, 'DongABank', 'https://api.vietqr.io/img/DOB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/DOB.svg', 'EACBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7720-842a-c70c143901c7', '74664d52-e0fb-4c56-b157-7b7506bdaeba', 'DBS Bank Ltd - Chi nhánh Thành phố Hồ Chí Minh', 'DBS', 796500, 'DBSBank', 'https://api.vietqr.io/img/DBS.png', 'https://cdn.banklookup.net/assets/images/bank-icons/DBS.svg', 'DBSSVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7721-a60d-9019f95830dc', '017e81d5-2cb8-4cc6-8444-30e9479aeb9b', 'Ngân hàng TMCP Xuất Nhập khẩu Việt Nam', 'EIB', 970431, 'Eximbank', 'https://api.vietqr.io/img/EIB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/EIB.svg', 'EBVIVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7722-96e5-2b11fe930039', 'd935586a-92d0-423d-9623-a8567d668879', 'Ngân hàng Thương mại TNHH MTV Dầu Khí Toàn Cầu', 'GPB', 970408, 'GPBank', 'https://api.vietqr.io/img/GPB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/GPB.svg', 'GBNKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7723-afa6-2919b20699cd', 'c8068add-c733-4f5e-a34b-c598669d9aee', 'Ngân hàng TMCP Phát triển Thành phố Hồ Chí Minh', 'HDB', 970437, 'HDBank', 'https://api.vietqr.io/img/HDB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/HDB.svg', 'HDBCVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7724-9911-f6c0f69bc4ab', '6e0b0ed8-82b3-47fd-982a-fdae96b327a9', 'Ngân hàng TNHH MTV Hong Leong Việt Nam', 'HLB', 970442, 'Hong Leong Bank', 'https://api.vietqr.io/img/HLBVN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/HLBVN.svg', 'HLBBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7725-827f-6a324964e766', 'e23cdeab-344b-4e2a-bc22-7cb2d4075166', 'Ngân hàng TNHH MTV HSBC (Việt Nam)', 'HSBC', 458761, 'HSBC', 'https://api.vietqr.io/img/HSBC.png', 'https://cdn.banklookup.net/assets/images/bank-icons/HSBC.svg', 'HSBCVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7726-b4f8-43725c44cb4b', '47f8f83f-a86d-4d8f-aeab-213417b2f904', 'Ngân hàng Công nghiệp Hàn Quốc - Chi nhánh TP. Hồ Chí Minh', 'IBKHCM', 970456, 'IBKHCM', 'https://api.vietqr.io/img/IBK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/IBK - HCM.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7727-8ce8-3ae6aaff2256', '58f9e003-036e-4bbb-b476-8f513d5153d6', 'Ngân hàng Công nghiệp Hàn Quốc - Chi nhánh Hà Nội', 'IBKHN', 970455, 'IBKHN', 'https://api.vietqr.io/img/IBK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/IBK - HN.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7728-9070-53c401c0bb87', '3fbc766d-9a2e-4d80-80cf-3e62ca32abbc', 'Ngân hàng TNHH Indovina', 'IVB', 970434, 'Indovina Bank', 'https://api.vietqr.io/img/IVB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/IVB.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7729-a7f6-7e54ce65e8b7', '712627bb-9ab3-4d9c-b300-b58bf0bb5d14', 'Ngân hàng Đại chúng TNHH Kasikornbank', 'KB', 668888, 'Kasikornbank', 'https://api.vietqr.io/img/KBANK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/KBank.svg', 'KASIVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772a-96ff-a76cad63250c', '6ecbd4f5-6c85-4c07-ada4-5fa9eec49431', 'Ngân hàng Kookmin - Chi nhánh Thành phố Hồ Chí Minh', 'KBKHCM', 970463, 'KookminHCM', 'https://api.vietqr.io/img/KBHCM.png', 'https://cdn.banklookup.net/assets/images/bank-icons/KBHCM.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772b-b63b-c2c326e41f70', '6ff67324-2611-4941-861b-abcf7b88bdee', 'Ngân hàng Kookmin - Chi nhánh Hà Nội', 'KBKHN', 970462, 'KookminHN', 'https://api.vietqr.io/img/KBHN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/KBHN.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772c-83d6-6f9b94fab780', '8892bfab-b050-40a4-b32b-73d11089578f', 'Ngân hàng TMCP Kiên Long', 'KLB', 970452, 'KienLongBank', 'https://api.vietqr.io/img/KLB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/KLB.svg', 'KLBKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772d-b9f2-bf7e8e3cd031', '7d721f6b-504a-42b4-8405-41ca6830971d', 'Ngân hàng TMCP Lộc Phát Việt Nam', 'LPB', 970449, 'LPBank', 'https://api.vietqr.io/img/LPB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/LPB.svg', 'LVBKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772e-bd6e-0c8ee2f66463', 'fe05a74d-0b50-4569-8a78-d44648fc944c', 'Ngân hàng TMCP Quân đội', 'MB', 970422, 'MBBank', 'https://api.vietqr.io/img/MB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/MB.svg', 'MSCBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-772f-a7ee-89df6e060b79', 'bf9303e0-4048-4545-92e6-0702f212d1a0', 'Ngân hàng TMCP Hàng Hải', 'MSB', 970426, 'MSB', 'https://api.vietqr.io/img/MSB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/MSB.svg', 'MCOBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7730-b9bd-8b79a0eb35ef', '2a3b45ca-38bc-49cb-97c3-e1fdd8f389e9', 'Ngân hàng TMCP Nam Á', 'NAB', 970428, 'NamABank', 'https://api.vietqr.io/img/NAB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/NAB.svg', 'NAMAVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7731-9d2a-4d5ac85d6a85', '428d3f77-7b99-40c5-bb07-80d34eb2d6a9', 'Ngân hàng TMCP Quốc Dân', 'NCB', 970419, 'NCB', 'https://api.vietqr.io/img/NCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/NCB.svg', 'NVBAVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7732-bb93-48ab964ff361', '33777996-3ce0-4b6e-9b70-71004e97e4b8', 'Ngân hàng Nonghyup - Chi nhánh Hà Nội', 'NHB', 801011, 'Nonghyup', 'https://api.vietqr.io/img/NHB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/NHB HN.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7733-8ed0-56bfa0874dbc', 'f7195395-0634-4ead-8773-88139abc8275', 'Ngân hàng TMCP Phương Đông', 'OCB', 970448, 'OCB', 'https://api.vietqr.io/img/OCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/OCB.svg', 'ORCOVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7734-8214-1a93e27c0230', 'd764fad1-d25a-40fa-8008-f7ce7ef2c029', 'Ngân hàng Thương mại TNHH MTV Đại Dương', 'OJB', 970414, 'Oceanbank', 'https://api.vietqr.io/img/OCEANBANK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/Oceanbank.svg', 'OCBKUS3M', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7735-813a-4c069ab5474e', '60d111bf-c742-4a46-9990-5442c9ac44d8', 'Ngân hàng TNHH MTV Public Việt Nam', 'PBVN', 970439, 'PublicBank', 'https://api.vietqr.io/img/PBVN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/PBVN.svg', 'VIDPVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7736-816a-cb3afac129f1', '06d27ed6-d640-4201-8f2e-ee5ff37f4580', 'Ngân hàng TMCP Xăng dầu Petrolimex', 'PGB', 970430, 'PGBank', 'https://api.vietqr.io/img/PGB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/PGB.svg', 'PGBLVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7737-910e-447b1cc44653', 'df183cfc-c104-4ff7-9ab7-c164229376c1', 'Ngân hàng TMCP Đại Chúng Việt Nam', 'PVCB', 970412, 'PVcomBank', 'https://api.vietqr.io/img/PVCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/PVCB.svg', 'WBVNVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7738-be33-9d4532cb3e62', '85954e77-0754-479b-aac1-15bd283ebc3e', 'Ngân hàng TMCP Sài Gòn Thương Tín', 'SCB', 970403, 'Sacombank', 'https://api.vietqr.io/img/STB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/STB.svg', 'SGTTVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7739-a60d-d65a4a9c6021', '793b6bfc-54c9-4012-83a4-ec5d0a4582c2', 'Ngân hàng TNHH MTV Standard Chartered Bank Việt Nam', 'SCBVN', 970410, 'Standard Chartered VN', 'https://api.vietqr.io/img/SCVN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SCVN.svg', 'SCBLVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773a-aaf2-93cd1ed62025', 'b7af5470-9304-4878-80b4-f687c678f83b', 'Ngân hàng TMCP Đông Nam Á', 'SEAB', 970440, 'SeABank', 'https://api.vietqr.io/img/SEAB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SEAB.svg', 'SEAVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773b-8143-0217debaf11f', 'c970732d-c1ed-4703-9f6c-b46fc84239b2', 'Ngân hàng TMCP Sài Gòn Công Thương', 'SGB', 970400, 'SaigonBank', 'https://api.vietqr.io/img/SGICB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SGICB.svg', 'SBITVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773c-9751-586fc6313415', '01ca8729-2519-46f2-8c4a-b109e7a45292', 'Ngân hàng TMCP Sài Gòn', 'SGCB', 970429, 'SCB', 'https://api.vietqr.io/img/SCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SCB.svg', 'SACLVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773d-a8a9-4c5c0c36445a', '2fb52185-a3d3-4ed1-8c45-5f71a2842de1', 'Ngân hàng TMCP Sài Gòn - Hà Nội', 'SHB', 970443, 'SHB', 'https://api.vietqr.io/img/SHB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SHB.svg', 'SHBAVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773e-a424-5439ecdd87a0', '0540840b-7f2b-46c0-93d3-5040c042b737', 'Ngân hàng TNHH MTV Shinhan Việt Nam', 'SHBVN', 970424, 'ShinhanBank', 'https://api.vietqr.io/img/SHBVN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/SHBVN.svg', 'SHBKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-773f-82ce-4e8a7acd9f26', 'd9b10c0e-e96d-4550-ac4d-1117d03c8afc', 'Ngân hàng TMCP Kỹ thương Việt Nam', 'TCB', 970407, 'Techcombank', 'https://api.vietqr.io/img/TCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/TCB.svg', 'VTCBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7740-94de-314c11c0f2b1', 'fa941344-6a64-4438-9bd5-11b57077da18', 'Ngân hàng số Timo', 'TIMO', 963388, 'Timo', 'https://vietqr.net/portal-service/resources/icons/TIMO.png', 'https://cdn.banklookup.net/assets/images/bank-icons/TIMO.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7741-bdfc-0f08ef19bf88', 'b468746c-1843-4c73-a0d9-70241c7cd8f6', 'Ngân hàng TMCP Tiên Phong', 'TPB', 970423, 'TPBank', 'https://api.vietqr.io/img/TPB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/TPB.svg', 'TPBVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7742-aad8-9e025d588070', '7b2a7fe7-5477-4b62-a9dc-93e2ba899fe0', 'TMCP Việt Nam Thịnh Vượng - Ngân hàng số Ubank by VPBank', 'UB', 546035, 'Ubank', 'https://api.vietqr.io/img/UBANK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/Ubank.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7743-9909-19e181e9ce1e', '913a0253-2a29-4918-8ed3-755162a73c23', 'Ngân hàng United Overseas', 'UOB', 970458, 'United Overseas Bank', 'https://api.vietqr.io/img/UOB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/UOB.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7744-a4d8-a595bd6704a3', 'dd586b22-8367-4439-95c8-98423a762ced', 'Ngân hàng TMCP Việt Á', 'VAB', 970427, 'VietABank', 'https://api.vietqr.io/img/VAB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VAB.svg', 'VNACVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7745-be9d-c9bdaf279a4a', '3cdde0d0-44f0-4cd4-9953-391fee3ec04e', 'Ngân hàng Nông nghiệp và Phát triển Nông thôn Việt Nam', 'VARB', 970405, 'Agribank', 'https://api.vietqr.io/img/VBA.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VBA.svg', 'VBAAVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7746-8932-d1fb8352b928', 'bf313f9e-c658-46e3-8d0b-530e013bd76d', 'Ngân hàng TMCP Việt Nam Thương Tín', 'VB', 970433, 'VietBank', 'https://api.vietqr.io/img/VIETBANK.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VIETBANK.svg', 'VNTTVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7747-97f6-48d7c86fda5c', 'cc2fa1a9-6ad5-4cc8-aee7-84a8cde9f62e', 'Ngân hàng TMCP Ngoại Thương Việt Nam', 'VCB', 970436, 'Vietcombank', 'https://api.vietqr.io/img/VCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VCB.svg', 'BFTVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7748-bccf-c3e6619f0285', 'ec514a5b-1cd7-4d7c-bf97-c6c412bed60c', 'Ngân hàng TMCP Bản Việt', 'VCCB', 970454, 'Ngân hàng Bản Việt', 'https://api.vietqr.io/img/VCCB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VCCB.svg', 'VCBCVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-7749-ab72-a8f3aa2cb71c', '5e1b6cfa-d5a7-4aae-9e59-dccc008c4eef', 'Ngân hàng TMCP Quốc tế Việt Nam', 'VIB', 970441, 'VIB', 'https://api.vietqr.io/img/VIB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VIB.svg', 'VNIBVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774a-987d-3b41f9b0ae42', '69256b98-d6a2-46ea-a62e-90b02c8eac00', 'VNPT Money', 'VNPTMONEY', 971011, 'VNPTMoney', 'https://api.vietqr.io/img/VNPTMONEY.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VNPTMONEY.svg', '', 0, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774b-abfb-945ae61909c4', '20351d8b-65ab-4cc4-90c2-b177d9c871b2', 'Ngân hàng TMCP Việt Nam Thịnh Vượng', 'VPB', 970432, 'VPBank', 'https://api.vietqr.io/img/VPB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VPB.svg', 'VPBKVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774c-ac6a-9f83d5bc3a5a', 'e01d061b-f468-4480-9078-0524083d3f09', 'Ngân hàng Liên doanh Việt - Nga', 'VRB', 970421, 'VRB', 'https://api.vietqr.io/img/VRB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VRB.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774d-a4f6-50b32452cffd', '6fd6330a-2044-4254-9619-2e012c560b92', 'Ngân hàng TMCP Công thương Việt Nam', 'VTB', 970415, 'VietinBank', 'https://api.vietqr.io/img/ICB.png', 'https://cdn.banklookup.net/assets/images/bank-icons/ICB.svg', 'ICBVVNVX', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774e-a37b-a25bb89c0bca', 'c0da3fe8-c530-410b-97f9-4af1d0d5c1c5', 'Viettel Money', 'VTLMONEY', 971005, 'ViettelMoney', 'https://api.vietqr.io/img/VIETTELMONEY.png', 'https://cdn.banklookup.net/assets/images/bank-icons/VTLMONEY.svg', '', 1, NULL, NULL, false);
INSERT INTO public."BankInfos" VALUES ('01968046-c450-774f-ab93-5716b68bf27d', 'bc286db1-9f40-417b-a3d7-494153fbeb2d', 'Ngân hàng TNHH MTV Woori Việt Nam', 'WOO', 970457, 'Woori', 'https://api.vietqr.io/img/WVN.png', 'https://cdn.banklookup.net/assets/images/bank-icons/WVN.svg', '', 1, NULL, NULL, false);


--
-- Data for Name: EncryptionKeys; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."EncryptionKeys" VALUES ('01968046-c461-73bb-b5e4-58491fee0f23', 'e2mzr2z7l61uact2u70OViS9xsAum/zV4RtzZvSUtKmUrhk2BBYFtTwV3Q/L4bB5kYASPOdLkLElb1bpf+gt6A==', 'CSzY4/a+N2Icz/oM3cknpg==', NULL, NULL, false);
INSERT INTO public."EncryptionKeys" VALUES ('01968046-c462-7772-aa3a-b97f4c2a5a2a', 'ygGkfVmK5s8kv8JDDVHN/5BO/q5eAIUjxcc6DsHaRC2M/mHvKtOOzcotlxUgjuCKrh8rycZfvYRV/u9oiITcSA==', '0xKYx8dBTRpHZWGZ91tcxQ==', NULL, NULL, false);
INSERT INTO public."EncryptionKeys" VALUES ('01968046-c462-7774-821d-f7606754ccb4', 'LgaUBBT7Ou3w0fS9VkztKXnRsysvKlfZ4gBDz/OAf6sJwH8tz8F52R+6+aG1pfWS5RLgPq8i+lLL34lbkKtQzQ==', 'aQaBULF5DnniwaEXJK9QtQ==', NULL, NULL, false);


--
-- Data for Name: UserRoles; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."UserRoles" VALUES ('01951e20-7a6e-7106-a6f3-148b63f52149', 'Owner', NULL, NULL, false);
INSERT INTO public."UserRoles" VALUES ('01951e20-ab3f-722f-aceb-3485c166e8cf', 'Driver', NULL, NULL, false);
INSERT INTO public."UserRoles" VALUES ('01951e22-c88e-7c99-901e-23ff1ebccf85', 'Admin', NULL, NULL, false);
INSERT INTO public."UserRoles" VALUES ('01951e22-dd78-7933-b742-76110d88728c', 'Consultant', NULL, NULL, false);
INSERT INTO public."UserRoles" VALUES ('01951e22-ee2e-7bbf-914e-e39f14e0f420', 'Technician', NULL, NULL, false);


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Users" VALUES ('01951ead-d228-7d1d-9174-d9e84d69c119', '01968046-c461-73bb-b5e4-58491fee0f23', '01951e22-c88e-7c99-901e-23ff1ebccf85', '', 'ADMIN', 'admin@gmail.com', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', '480/59A, Đường Bình Quới, Phường 28, Quận Bình Thạnh', '2025-04-29 06:40:43.607521+00', 'stltJZhquQ0MX319YDzu4g==', 0, 0, '', '', '', NULL, NULL, NULL, NULL, NULL, false, '', NULL, NULL, false);
INSERT INTO public."Users" VALUES ('01951eae-453b-7ad9-949f-63dd30b592e1', '01968046-c462-7772-aa3a-b97f4c2a5a2a', '01951e22-ee2e-7bbf-914e-e39f14e0f420', '', 'Technician', 'technician@gmail.com', '29ba9d9cef5a66461116a24938bb9307e005c35aa1bb909f16aa5e85bd767480', '480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh', '2025-04-29 06:40:43.608003+00', '8TIILeD08tNwZQHp6VAznw==', 0, 0, '', '', '', NULL, NULL, NULL, NULL, NULL, false, '', NULL, NULL, false);
INSERT INTO public."Users" VALUES ('01951eae-7342-78fb-ae8c-02ce503ed400', '01968046-c462-7774-821d-f7606754ccb4', '01951e22-dd78-7933-b742-76110d88728c', '', 'Consultant', 'consultant@gmail.com', '4087b6426d9adf2bd519bbadbf0b3249274c7fc23b8347efe29f188bb7a4878b', '480/59A Đường Bình Quới Phường 28 Quận Bình Thạnh', '2025-04-29 06:40:43.60801+00', 'dOAt7HYy/2WWMASV5Zxxlg==', 0, 0, '', '', '', NULL, NULL, NULL, NULL, NULL, false, '', NULL, NULL, false);


--
-- Data for Name: BankAccounts; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: FuelTypes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."FuelTypes" VALUES ('01968046-c450-7750-92cb-a86e37d56a91', 'Xăng', NULL, NULL, false);
INSERT INTO public."FuelTypes" VALUES ('01968046-c450-7751-be6d-a3c82673f492', 'Dầu', NULL, NULL, false);
INSERT INTO public."FuelTypes" VALUES ('01968046-c450-7752-82a0-ae28184e2b6a', 'Điện', NULL, NULL, false);
INSERT INTO public."FuelTypes" VALUES ('01968046-c450-7753-85ec-4f598889d34c', 'Hybrid', NULL, NULL, false);


--
-- Data for Name: Manufacturers; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7167-99cd-271c6e4b3e2e', 'Toyota', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7169-ae45-c4112fea23ab', 'Honda', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-716b-9e95-64406ad946a4', 'Ford', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-716d-abb7-ddbcf381a1d0', 'Chevrolet', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-716f-a56d-8c9665b59b69', 'Mercedes-Benz', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7171-9d51-db12c9bd024d', 'BMW', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7173-810e-34b03a52f86a', 'Audi', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7175-b699-4bb50c1eb891', 'Volkswagen', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7177-b204-1ea0641e44e0', 'Hyundai', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7179-9b49-7998edb6d0fa', 'Kia', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-717b-9041-a0bc70f0bb14', 'Mazda', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-717d-b219-4f539862abc0', 'Nissan', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-717f-a558-0fbd97874c9d', 'Subaru', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7181-ab06-da48818b8f4f', 'Tesla', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7183-ad35-c149bafc6979', 'Volvo', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7185-af01-47013591aad5', 'Porsche', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7187-bb8c-a722dfb256b7', 'Jaguar', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7189-9f75-acc3ac0059a4', 'Lexus', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-718b-b6a3-8de47d140576', 'Land Rover', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-718d-8add-32f04acd265b', 'Ferrari', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-718f-9048-d80d37efa260', 'Lamborghini', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7191-8228-d3aa446a870f', 'Bugatti', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7193-8a01-b68783e7836d', 'McLaren', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7195-ad88-774a94752c2f', 'Rolls-Royce', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7197-affa-fd141746822a', 'Bentley', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-7199-bfb8-0252b5250ddc', 'Peugeot', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-719b-a5f1-ec9729aead6f', 'Renault', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-719d-a97e-c5243d33d6c4', 'Citroën', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-719f-9ba6-61fb21044750', 'Fiat', '', NULL, NULL, false);
INSERT INTO public."Manufacturers" VALUES ('01968046-c452-71a1-81d0-e4b98f433aab', 'Jeep', '', NULL, NULL, false);


--
-- Data for Name: Models; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Models" VALUES ('01968046-c455-70b2-a497-4c821a79f41e', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Camry', '2020-04-29 06:40:43.606006+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74a2-9e5f-6259c22046de', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Corolla', '2020-04-29 06:40:43.606042+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74a4-80bc-55e22c87d168', '01968046-c452-7167-99cd-271c6e4b3e2e', 'RAV4', '2020-04-29 06:40:43.606045+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74a6-b954-7aa01a36fe0e', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Highlander', '2020-04-29 06:40:43.606046+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74a8-822b-88980112d482', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Land Cruiser', '2016-04-29 06:40:43.606047+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74aa-9f04-d6459ccb4d82', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Fortuner', '2019-04-29 06:40:43.606047+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ac-b3aa-9da2df71bb06', '01968046-c452-7167-99cd-271c6e4b3e2e', 'Innova', '2023-04-29 06:40:43.606048+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ae-beea-18770ac1d913', '01968046-c452-7169-ae45-c4112fea23ab', 'Civic', '2023-04-29 06:40:43.606049+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74b0-81a2-a506f872e854', '01968046-c452-7169-ae45-c4112fea23ab', 'Accord', '2024-04-29 06:40:43.60605+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74b2-b372-e2e8b6bb72e7', '01968046-c452-7169-ae45-c4112fea23ab', 'CR-V', '2017-04-29 06:40:43.60605+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74b4-8d86-a1b2ac786d5b', '01968046-c452-7169-ae45-c4112fea23ab', 'HR-V', '2024-04-29 06:40:43.606051+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74b6-becb-5715b8ee94b8', '01968046-c452-7169-ae45-c4112fea23ab', 'City', '2021-04-29 06:40:43.606052+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74b8-abcb-f386c9918741', '01968046-c452-7169-ae45-c4112fea23ab', 'Jazz', '2024-04-29 06:40:43.606052+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ba-8419-c614d78ce858', '01968046-c452-7169-ae45-c4112fea23ab', 'Pilot', '2019-04-29 06:40:43.606053+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74bc-bddf-cd80f55c8ba6', '01968046-c452-716b-9e95-64406ad946a4', 'Mustang', '2020-04-29 06:40:43.606053+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74be-9cbc-756e84ece92a', '01968046-c452-716b-9e95-64406ad946a4', 'F-150', '2022-04-29 06:40:43.606054+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74c0-b506-a20f70bbd3f6', '01968046-c452-716b-9e95-64406ad946a4', 'Explorer', '2022-04-29 06:40:43.606055+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74c2-9752-b7da5eb28445', '01968046-c452-716b-9e95-64406ad946a4', 'Escape', '2021-04-29 06:40:43.606056+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74c4-97d7-6c056769d9ad', '01968046-c452-716b-9e95-64406ad946a4', 'Ranger', '2025-04-29 06:40:43.606057+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74c6-848b-06482f208889', '01968046-c452-716b-9e95-64406ad946a4', 'Focus', '2024-04-29 06:40:43.606058+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74c8-a119-af16382ee9f1', '01968046-c452-716b-9e95-64406ad946a4', 'Everest', '2022-04-29 06:40:43.606058+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ca-bc1b-a3e9fa4ac200', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Silverado', '2025-04-29 06:40:43.606059+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74cc-933d-6474453fc5a8', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Suburban', '2024-04-29 06:40:43.606059+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ce-86fd-7509eae0be25', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Tahoe', '2022-04-29 06:40:43.60606+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74d0-8416-928c210b391f', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Camaro', '2017-04-29 06:40:43.606061+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74d2-85d8-c1e25dfa7bc4', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Corvette', '2018-04-29 06:40:43.606061+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74d4-aa75-ebd83e477de7', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Malibu', '2018-04-29 06:40:43.606062+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74d6-b8b2-5b700d55f3c0', '01968046-c452-716d-abb7-ddbcf381a1d0', 'Equinox', '2019-04-29 06:40:43.606062+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74d8-90e4-3cb01b6d9792', '01968046-c452-716f-a56d-8c9665b59b69', 'C-Class', '2016-04-29 06:40:43.606063+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74da-a615-316c92d0d8ea', '01968046-c452-716f-a56d-8c9665b59b69', 'E-Class', '2017-04-29 06:40:43.606064+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74dc-830c-25dd7deb6c5b', '01968046-c452-716f-a56d-8c9665b59b69', 'S-Class', '2020-04-29 06:40:43.606064+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74de-8f65-90d3f2832630', '01968046-c452-716f-a56d-8c9665b59b69', 'GLC', '2019-04-29 06:40:43.606065+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74e0-b1f0-92ac73866064', '01968046-c452-716f-a56d-8c9665b59b69', 'GLE', '2016-04-29 06:40:43.606065+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74e2-9ef9-978f24dd63de', '01968046-c452-716f-a56d-8c9665b59b69', 'GLA', '2023-04-29 06:40:43.606066+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74e4-bfde-16e9c170d2d2', '01968046-c452-716f-a56d-8c9665b59b69', 'AMG GT', '2021-04-29 06:40:43.606067+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74e6-afd6-76559e003df6', '01968046-c452-7171-9d51-db12c9bd024d', '3 Series', '2022-04-29 06:40:43.606067+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74e8-a27d-db0fc6412a16', '01968046-c452-7171-9d51-db12c9bd024d', '5 Series', '2022-04-29 06:40:43.606068+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ea-bc10-7bed4379084c', '01968046-c452-7171-9d51-db12c9bd024d', '7 Series', '2022-04-29 06:40:43.606068+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ec-83b1-f374c96a1c93', '01968046-c452-7171-9d51-db12c9bd024d', 'X3', '2022-04-29 06:40:43.606069+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74ee-91c9-e5b30a820181', '01968046-c452-7171-9d51-db12c9bd024d', 'X5', '2023-04-29 06:40:43.60607+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74f0-92b7-bc3a67c05acd', '01968046-c452-7171-9d51-db12c9bd024d', 'M3', '2016-04-29 06:40:43.60607+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74f2-8857-945f321b3f7c', '01968046-c452-7171-9d51-db12c9bd024d', 'M5', '2019-04-29 06:40:43.606071+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74f4-84b4-474cde434f1e', '01968046-c452-7173-810e-34b03a52f86a', 'A4', '2024-04-29 06:40:43.606071+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74f6-858f-41fc61bc2f90', '01968046-c452-7173-810e-34b03a52f86a', 'A6', '2018-04-29 06:40:43.606073+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74f8-8b89-e69392e5900e', '01968046-c452-7173-810e-34b03a52f86a', 'A8', '2018-04-29 06:40:43.606073+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74fa-bbc4-32bb6f149584', '01968046-c452-7173-810e-34b03a52f86a', 'Q5', '2021-04-29 06:40:43.606074+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74fc-a994-8f3b1f6899b4', '01968046-c452-7173-810e-34b03a52f86a', 'Q7', '2020-04-29 06:40:43.606076+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-74fe-b2d5-8b9f1c1739e7', '01968046-c452-7173-810e-34b03a52f86a', 'RS6', '2019-04-29 06:40:43.606076+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7500-bf4a-8ab13321ba9b', '01968046-c452-7173-810e-34b03a52f86a', 'R8', '2020-04-29 06:40:43.606077+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7502-a6d6-7f59041b5ef1', '01968046-c452-7175-b699-4bb50c1eb891', 'Golf', '2017-04-29 06:40:43.606077+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7504-85bb-def02ea22c4c', '01968046-c452-7175-b699-4bb50c1eb891', 'Passat', '2021-04-29 06:40:43.606078+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7506-8e83-3328ddfb0595', '01968046-c452-7175-b699-4bb50c1eb891', 'Tiguan', '2023-04-29 06:40:43.606079+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7508-b168-0550f103fcc4', '01968046-c452-7175-b699-4bb50c1eb891', 'Atlas', '2018-04-29 06:40:43.606079+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-750a-a391-8f17b7c2b4d2', '01968046-c452-7175-b699-4bb50c1eb891', 'Jetta', '2023-04-29 06:40:43.606081+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-750c-87c1-f653c6cc7188', '01968046-c452-7175-b699-4bb50c1eb891', 'Arteon', '2020-04-29 06:40:43.606081+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-750e-8457-c0f4629bf559', '01968046-c452-7175-b699-4bb50c1eb891', 'ID.4', '2017-04-29 06:40:43.606082+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7510-9b7e-f7fddb0b2856', '01968046-c452-7177-b204-1ea0641e44e0', 'Elantra', '2024-04-29 06:40:43.606082+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7512-a2f8-feba59da8de6', '01968046-c452-7177-b204-1ea0641e44e0', 'Sonata', '2022-04-29 06:40:43.606083+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7514-9591-d70c71bbdae5', '01968046-c452-7177-b204-1ea0641e44e0', 'Tucson', '2021-04-29 06:40:43.606083+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7516-8a2e-bef6a3531669', '01968046-c452-7177-b204-1ea0641e44e0', 'Santa Fe', '2024-04-29 06:40:43.606084+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7518-a49e-7ed421d66f58', '01968046-c452-7177-b204-1ea0641e44e0', 'Palisade', '2021-04-29 06:40:43.606085+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-751a-bd60-e20bd6b38817', '01968046-c452-7177-b204-1ea0641e44e0', 'Kona', '2018-04-29 06:40:43.606085+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-751c-83c5-3f622a29b20e', '01968046-c452-7177-b204-1ea0641e44e0', 'Venue', '2016-04-29 06:40:43.606086+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-751e-9491-2bf7f89d1662', '01968046-c452-7179-9b49-7998edb6d0fa', 'Forte', '2020-04-29 06:40:43.606087+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7520-bb83-fa1070a5f3f4', '01968046-c452-7179-9b49-7998edb6d0fa', 'K5', '2021-04-29 06:40:43.606087+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7522-a547-ed7ae5c415cc', '01968046-c452-7179-9b49-7998edb6d0fa', 'Sportage', '2025-04-29 06:40:43.606088+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7524-93f6-f6806a0bcd6a', '01968046-c452-7179-9b49-7998edb6d0fa', 'Telluride', '2020-04-29 06:40:43.606088+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7526-8399-111e9f147df5', '01968046-c452-7179-9b49-7998edb6d0fa', 'Sorento', '2025-04-29 06:40:43.606089+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7528-a65e-365af81a500b', '01968046-c452-7179-9b49-7998edb6d0fa', 'Carnival', '2019-04-29 06:40:43.60609+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-752a-bc9f-abb19fd9c343', '01968046-c452-7179-9b49-7998edb6d0fa', 'EV6', '2018-04-29 06:40:43.60609+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-752c-ae6d-dd77020a364e', '01968046-c452-717b-9041-a0bc70f0bb14', 'Mazda3', '2023-04-29 06:40:43.606091+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-752e-b0d4-b9023c7c57bb', '01968046-c452-717b-9041-a0bc70f0bb14', 'Mazda6', '2023-04-29 06:40:43.606091+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7530-87d9-9b6e711b6044', '01968046-c452-717b-9041-a0bc70f0bb14', 'CX-5', '2019-04-29 06:40:43.606092+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7532-9f77-056c7a915e18', '01968046-c452-717b-9041-a0bc70f0bb14', 'CX-9', '2023-04-29 06:40:43.606093+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7534-a53f-2e3045cceb0f', '01968046-c452-717b-9041-a0bc70f0bb14', 'MX-5', '2016-04-29 06:40:43.606093+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7536-9176-a31f4d20a956', '01968046-c452-717b-9041-a0bc70f0bb14', 'CX-30', '2018-04-29 06:40:43.606094+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7538-8a26-025442e477aa', '01968046-c452-717b-9041-a0bc70f0bb14', 'CX-50', '2020-04-29 06:40:43.606094+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-753a-9510-c49f09c7a83e', '01968046-c452-717d-b219-4f539862abc0', 'Altima', '2017-04-29 06:40:43.606095+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-753c-82d4-d77694a25b07', '01968046-c452-717d-b219-4f539862abc0', 'Maxima', '2025-04-29 06:40:43.606096+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-753e-b02b-511c345e8c6a', '01968046-c452-717d-b219-4f539862abc0', 'Rogue', '2019-04-29 06:40:43.606096+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7540-91c5-067904fe883e', '01968046-c452-717d-b219-4f539862abc0', 'Pathfinder', '2021-04-29 06:40:43.606097+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7542-9c37-e24dfb93e4c5', '01968046-c452-717d-b219-4f539862abc0', 'GT-R', '2021-04-29 06:40:43.606097+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7544-9bc2-e5e414c1eda2', '01968046-c452-717d-b219-4f539862abc0', 'Z', '2021-04-29 06:40:43.606098+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7546-96d5-42167160be96', '01968046-c452-717d-b219-4f539862abc0', 'Ariya', '2020-04-29 06:40:43.606099+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7548-aba8-d97b40e42b0a', '01968046-c452-717f-a558-0fbd97874c9d', 'Impreza', '2018-04-29 06:40:43.6061+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-754a-ac78-c4ee1f9df55e', '01968046-c452-717f-a558-0fbd97874c9d', 'Legacy', '2021-04-29 06:40:43.606101+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-754c-85e9-6183029ba485', '01968046-c452-717f-a558-0fbd97874c9d', 'Outback', '2019-04-29 06:40:43.606102+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-754e-b83e-1d2399640266', '01968046-c452-717f-a558-0fbd97874c9d', 'Forester', '2024-04-29 06:40:43.606102+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7550-94d8-284027dad5e9', '01968046-c452-717f-a558-0fbd97874c9d', 'Crosstrek', '2017-04-29 06:40:43.606103+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7552-bfa2-ba30fcb9241d', '01968046-c452-717f-a558-0fbd97874c9d', 'WRX', '2023-04-29 06:40:43.606103+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7554-97d3-401ab0911f51', '01968046-c452-717f-a558-0fbd97874c9d', 'BRZ', '2025-04-29 06:40:43.606104+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7556-9910-e988d8fe18ae', '01968046-c452-7181-ab06-da48818b8f4f', 'Model 3', '2016-04-29 06:40:43.606105+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7558-afb7-8aef0e581193', '01968046-c452-7181-ab06-da48818b8f4f', 'Model S', '2021-04-29 06:40:43.606105+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-755a-bca9-5b1d44d830e6', '01968046-c452-7181-ab06-da48818b8f4f', 'Model X', '2019-04-29 06:40:43.606106+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-755c-a892-bbc8d15ee8ab', '01968046-c452-7181-ab06-da48818b8f4f', 'Model Y', '2020-04-29 06:40:43.606106+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-755e-952d-263ab0505cc8', '01968046-c452-7181-ab06-da48818b8f4f', 'Cybertruck', '2024-04-29 06:40:43.606107+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7560-8c49-a44d64ef59af', '01968046-c452-7183-ad35-c149bafc6979', 'S60', '2020-04-29 06:40:43.606108+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7562-98a8-06fc10b63185', '01968046-c452-7183-ad35-c149bafc6979', 'S90', '2023-04-29 06:40:43.606108+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7564-8742-1ac87ded1a88', '01968046-c452-7183-ad35-c149bafc6979', 'XC40', '2023-04-29 06:40:43.606109+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7566-9648-2899101fb4b9', '01968046-c452-7183-ad35-c149bafc6979', 'XC60', '2016-04-29 06:40:43.606109+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7568-b0ee-3919794bf5ee', '01968046-c452-7183-ad35-c149bafc6979', 'XC90', '2019-04-29 06:40:43.60611+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-756a-ac9f-61e7a025e955', '01968046-c452-7183-ad35-c149bafc6979', 'C40', '2022-04-29 06:40:43.606111+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-756c-9f5e-569cbb89452c', '01968046-c452-7183-ad35-c149bafc6979', 'V60', '2025-04-29 06:40:43.606112+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-756e-a83e-daab4edb5626', '01968046-c452-7185-af01-47013591aad5', '911', '2019-04-29 06:40:43.606113+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7570-8e4c-7660753171e5', '01968046-c452-7185-af01-47013591aad5', 'Cayenne', '2024-04-29 06:40:43.606113+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7572-8b69-6171c323c793', '01968046-c452-7185-af01-47013591aad5', 'Panamera', '2016-04-29 06:40:43.606114+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7574-9f84-8ddecb2056bc', '01968046-c452-7185-af01-47013591aad5', 'Macan', '2023-04-29 06:40:43.606114+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7576-bc30-4e67c7624f11', '01968046-c452-7185-af01-47013591aad5', 'Taycan', '2017-04-29 06:40:43.606115+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7578-9e9c-e5f645f9d9c5', '01968046-c452-7185-af01-47013591aad5', '718 Cayman', '2019-04-29 06:40:43.606116+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-757a-a843-eead5a34dc68', '01968046-c452-7187-bb8c-a722dfb256b7', 'F-TYPE', '2017-04-29 06:40:43.606116+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-757c-95e1-f466e619e9e6', '01968046-c452-7187-bb8c-a722dfb256b7', 'XF', '2017-04-29 06:40:43.606117+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-757e-b7a4-4c68d8b13b83', '01968046-c452-7187-bb8c-a722dfb256b7', 'XE', '2024-04-29 06:40:43.606117+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7580-9a06-3fd9dfb0b51e', '01968046-c452-7187-bb8c-a722dfb256b7', 'F-PACE', '2023-04-29 06:40:43.606118+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7582-ae08-0330967761a0', '01968046-c452-7187-bb8c-a722dfb256b7', 'E-PACE', '2017-04-29 06:40:43.606118+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7584-8d47-b9fba73153e5', '01968046-c452-7187-bb8c-a722dfb256b7', 'I-PACE', '2020-04-29 06:40:43.606119+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7586-86db-c110b6365a55', '01968046-c452-7187-bb8c-a722dfb256b7', 'XJ', '2021-04-29 06:40:43.60612+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7588-9510-a478d44d7749', '01968046-c452-7189-9f75-acc3ac0059a4', 'ES', '2017-04-29 06:40:43.60612+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-758a-862a-366c08b16611', '01968046-c452-7189-9f75-acc3ac0059a4', 'IS', '2023-04-29 06:40:43.606121+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-758c-9523-a93e5b9f47c9', '01968046-c452-7189-9f75-acc3ac0059a4', 'LS', '2017-04-29 06:40:43.606121+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-758e-a62a-0d31490c715a', '01968046-c452-7189-9f75-acc3ac0059a4', 'RX', '2016-04-29 06:40:43.606122+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7590-8132-d7997ca8a458', '01968046-c452-7189-9f75-acc3ac0059a4', 'NX', '2024-04-29 06:40:43.606123+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7592-b438-c1a80b9cfbc8', '01968046-c452-7189-9f75-acc3ac0059a4', 'GX', '2018-04-29 06:40:43.606123+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7594-ac14-9099e1120e63', '01968046-c452-7189-9f75-acc3ac0059a4', 'LX', '2019-04-29 06:40:43.606124+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7596-8009-60e136bcf9fb', '01968046-c452-718b-b6a3-8de47d140576', 'Range Rover', '2024-04-29 06:40:43.606125+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7598-87b3-7b379029dcd8', '01968046-c452-718b-b6a3-8de47d140576', 'Discovery', '2022-04-29 06:40:43.606125+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-759a-9a76-25feb87f0a65', '01968046-c452-718b-b6a3-8de47d140576', 'Defender', '2022-04-29 06:40:43.606126+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-759c-8bf6-80baef6d8514', '01968046-c452-718b-b6a3-8de47d140576', 'Velar', '2020-04-29 06:40:43.606126+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-759e-9cd4-68dbd3c360a0', '01968046-c452-718b-b6a3-8de47d140576', 'Evoque', '2022-04-29 06:40:43.606127+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75a0-8adf-21f264a228cd', '01968046-c452-718b-b6a3-8de47d140576', 'Sport', '2022-04-29 06:40:43.606127+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75a2-814b-fd405848d9ed', '01968046-c452-718d-8add-32f04acd265b', 'F8 Tributo', '2024-04-29 06:40:43.606129+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75a4-945d-dd30f048119c', '01968046-c452-718d-8add-32f04acd265b', 'SF90 Stradale', '2022-04-29 06:40:43.606129+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75a6-9db9-28c829c607df', '01968046-c452-718d-8add-32f04acd265b', '812 Superfast', '2025-04-29 06:40:43.60613+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75a8-87a5-35128659bbf9', '01968046-c452-718d-8add-32f04acd265b', 'Roma', '2021-04-29 06:40:43.606131+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75aa-8dfe-9ce86269cb5d', '01968046-c452-718d-8add-32f04acd265b', 'Portofino', '2023-04-29 06:40:43.606131+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ac-85ea-d11b8b11a774', '01968046-c452-718f-9048-d80d37efa260', 'Huracán', '2018-04-29 06:40:43.606132+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ae-8cd4-0de5248184b1', '01968046-c452-718f-9048-d80d37efa260', 'Aventador', '2025-04-29 06:40:43.606133+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75b0-83e0-57f8ca0d3607', '01968046-c452-718f-9048-d80d37efa260', 'Urus', '2018-04-29 06:40:43.606133+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75b2-8a5b-c44639e3da37', '01968046-c452-718f-9048-d80d37efa260', 'Revuelto', '2019-04-29 06:40:43.606134+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75b4-b4a8-2fa085707c0c', '01968046-c452-718f-9048-d80d37efa260', 'Gallardo', '2018-04-29 06:40:43.606134+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75b6-81f3-3d809d958215', '01968046-c452-7191-8228-d3aa446a870f', 'Chiron', '2016-04-29 06:40:43.606135+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75b8-bf8d-6da6f50cf8ae', '01968046-c452-7191-8228-d3aa446a870f', 'Veyron', '2018-04-29 06:40:43.606136+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ba-88f9-3ac93348eb61', '01968046-c452-7191-8228-d3aa446a870f', 'Divo', '2024-04-29 06:40:43.606136+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75bc-b896-72b3c3554272', '01968046-c452-7191-8228-d3aa446a870f', 'Centodieci', '2018-04-29 06:40:43.606137+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75be-81fa-fe0e210563d5', '01968046-c452-7191-8228-d3aa446a870f', 'Mistral', '2025-04-29 06:40:43.606137+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75c0-9412-e846b83d7498', '01968046-c452-7193-8a01-b68783e7836d', '720S', '2016-04-29 06:40:43.606138+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75c2-bac8-ac53136eb7e0', '01968046-c452-7193-8a01-b68783e7836d', '765LT', '2021-04-29 06:40:43.606138+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75c4-9aef-c8f7a571dcab', '01968046-c452-7193-8a01-b68783e7836d', 'Artura', '2018-04-29 06:40:43.606139+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75c6-bf38-aa6cf108dfe0', '01968046-c452-7193-8a01-b68783e7836d', 'GT', '2023-04-29 06:40:43.60614+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75c8-9f99-24f09af5e43c', '01968046-c452-7193-8a01-b68783e7836d', 'P1', '2018-04-29 06:40:43.60614+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ca-8f82-81ee9ab9b3be', '01968046-c452-7193-8a01-b68783e7836d', 'Senna', '2018-04-29 06:40:43.606141+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75cc-a7e8-e05c79976777', '01968046-c452-7193-8a01-b68783e7836d', 'Speedtail', '2020-04-29 06:40:43.606141+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ce-b43d-c17a36321e3f', '01968046-c452-7195-ad88-774a94752c2f', 'Phantom', '2021-04-29 06:40:43.606142+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75d0-8bfd-ff062f094190', '01968046-c452-7195-ad88-774a94752c2f', 'Ghost', '2025-04-29 06:40:43.606143+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75d2-9e87-09443c18b0b1', '01968046-c452-7195-ad88-774a94752c2f', 'Cullinan', '2019-04-29 06:40:43.606143+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75d4-abc7-4cad344223c0', '01968046-c452-7195-ad88-774a94752c2f', 'Wraith', '2023-04-29 06:40:43.606144+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75d6-b9be-93b989f5f620', '01968046-c452-7195-ad88-774a94752c2f', 'Dawn', '2023-04-29 06:40:43.606145+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75d8-bafc-d20db7459655', '01968046-c452-7195-ad88-774a94752c2f', 'Spectre', '2020-04-29 06:40:43.606145+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75da-9cb6-d552f83cef16', '01968046-c452-7197-affa-fd141746822a', 'Continental GT', '2020-04-29 06:40:43.606146+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75dc-b329-5f7447e22859', '01968046-c452-7197-affa-fd141746822a', 'Flying Spur', '2018-04-29 06:40:43.606146+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75de-b773-78a7e3415cc8', '01968046-c452-7197-affa-fd141746822a', 'Bentayga', '2025-04-29 06:40:43.606147+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75e0-bc59-fe8be6f1322e', '01968046-c452-7197-affa-fd141746822a', 'Mulsanne', '2017-04-29 06:40:43.606147+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75e2-90d3-45927756d4ba', '01968046-c452-7197-affa-fd141746822a', 'Bacalar', '2024-04-29 06:40:43.606148+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75e4-8813-61402ce7b98d', '01968046-c452-7199-bfb8-0252b5250ddc', '208', '2022-04-29 06:40:43.606149+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75e6-b857-1bd3065f6818', '01968046-c452-7199-bfb8-0252b5250ddc', '2008', '2016-04-29 06:40:43.606149+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75e8-89dd-c3a7dc236257', '01968046-c452-7199-bfb8-0252b5250ddc', '3008', '2023-04-29 06:40:43.60615+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ea-b961-5ab1f7ab2e03', '01968046-c452-7199-bfb8-0252b5250ddc', '5008', '2017-04-29 06:40:43.606151+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ec-bcd5-b6f950a635f5', '01968046-c452-7199-bfb8-0252b5250ddc', '508', '2019-04-29 06:40:43.606151+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75ee-bd3c-0edaeb7be291', '01968046-c452-7199-bfb8-0252b5250ddc', 'e-208', '2020-04-29 06:40:43.606152+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75f0-b331-64ae97d16917', '01968046-c452-7199-bfb8-0252b5250ddc', 'e-2008', '2016-04-29 06:40:43.606153+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75f2-bec5-3a1d7d778d8b', '01968046-c452-719b-a5f1-ec9729aead6f', 'Clio', '2019-04-29 06:40:43.606153+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75f4-8181-d7aa1beb549b', '01968046-c452-719b-a5f1-ec9729aead6f', 'Captur', '2017-04-29 06:40:43.606154+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75f6-b565-ff98f445cfec', '01968046-c452-719b-a5f1-ec9729aead6f', 'Megane', '2025-04-29 06:40:43.606155+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75f8-bf2d-d24602826db1', '01968046-c452-719b-a5f1-ec9729aead6f', 'Arkana', '2022-04-29 06:40:43.606155+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75fa-bd40-8b3345bcf7b3', '01968046-c452-719b-a5f1-ec9729aead6f', 'Austral', '2024-04-29 06:40:43.606156+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75fc-8646-ea1655289587', '01968046-c452-719b-a5f1-ec9729aead6f', 'Espace', '2017-04-29 06:40:43.606156+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-75fe-8ffb-39e5818c239b', '01968046-c452-719b-a5f1-ec9729aead6f', 'Scenic', '2024-04-29 06:40:43.606157+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7600-ba5b-b0a6a06c1bb1', '01968046-c452-719d-a97e-c5243d33d6c4', 'C3', '2019-04-29 06:40:43.606157+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7602-9e3a-72a89bb9a53b', '01968046-c452-719d-a97e-c5243d33d6c4', 'C4', '2016-04-29 06:40:43.606158+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7604-9379-ea6946e0bc0d', '01968046-c452-719d-a97e-c5243d33d6c4', 'C5 X', '2025-04-29 06:40:43.606159+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7606-8247-22e1945ca886', '01968046-c452-719d-a97e-c5243d33d6c4', 'ë-C4', '2025-04-29 06:40:43.606159+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7608-a6c0-ed7ab8660728', '01968046-c452-719d-a97e-c5243d33d6c4', 'C5 Aircross', '2016-04-29 06:40:43.60616+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-760a-a48b-6d7a928d2b55', '01968046-c452-719d-a97e-c5243d33d6c4', 'Berlingo', '2023-04-29 06:40:43.60616+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-760c-a9b2-6617109356f9', '01968046-c452-719d-a97e-c5243d33d6c4', 'SpaceTourer', '2021-04-29 06:40:43.606161+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-760e-8d48-90380568f899', '01968046-c452-719f-9ba6-61fb21044750', '500', '2021-04-29 06:40:43.606162+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7610-b409-b717efcb0170', '01968046-c452-719f-9ba6-61fb21044750', 'Panda', '2025-04-29 06:40:43.606162+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7612-92c9-953452289d32', '01968046-c452-719f-9ba6-61fb21044750', 'Tipo', '2021-04-29 06:40:43.606163+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7614-8e65-a77307ae3c41', '01968046-c452-719f-9ba6-61fb21044750', '500X', '2025-04-29 06:40:43.606163+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7616-bf6e-6b9a45078ff1', '01968046-c452-719f-9ba6-61fb21044750', '500L', '2019-04-29 06:40:43.606164+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7618-9711-9becac4f24a7', '01968046-c452-719f-9ba6-61fb21044750', '124 Spider', '2021-04-29 06:40:43.606165+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-761a-8ad4-943fed161cb2', '01968046-c452-719f-9ba6-61fb21044750', 'Ducato', '2021-04-29 06:40:43.606165+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-761c-826b-fcdf7abd6862', '01968046-c452-71a1-81d0-e4b98f433aab', 'Wrangler', '2019-04-29 06:40:43.606166+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-761e-93a8-5e0f998b2063', '01968046-c452-71a1-81d0-e4b98f433aab', 'Grand Cherokee', '2020-04-29 06:40:43.606166+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7620-93d8-d9bed6c83c80', '01968046-c452-71a1-81d0-e4b98f433aab', 'Cherokee', '2017-04-29 06:40:43.606167+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7622-9347-c57f9cecfb70', '01968046-c452-71a1-81d0-e4b98f433aab', 'Compass', '2022-04-29 06:40:43.606167+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7624-b26b-d0ce71635c19', '01968046-c452-71a1-81d0-e4b98f433aab', 'Renegade', '2020-04-29 06:40:43.606168+00', NULL, NULL, false);
INSERT INTO public."Models" VALUES ('01968046-c456-7626-9acd-a89308d9e9a5', '01968046-c452-71a1-81d0-e4b98f433aab', 'Gladiator', '2025-04-29 06:40:43.606169+00', NULL, NULL, false);


--
-- Data for Name: TransmissionTypes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."TransmissionTypes" VALUES ('01968046-c453-73e2-b3ac-fa1bc6234c61', 'Số Tự Động', NULL, NULL, false);
INSERT INTO public."TransmissionTypes" VALUES ('01968046-c453-73e3-bfc1-b43818def5fb', 'Số Sàn', NULL, NULL, false);


--
-- Data for Name: Cars; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Bookings; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: BookingLockedBalances; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: BookingReports; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarAmenities; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarAvailabilities; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: GPSDevices; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7627-8cdc-a71682a6b84c', 0, 'OSBuildId14', 'GPS-001', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7628-8498-01da609666cc', 0, 'OSBuildId2', 'GPS-002', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7629-85d2-0acc2e232a7f', 1, 'OSBuildId3', 'GPS-003', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762a-ae61-ff00ccfca5de', 2, 'OSBuildId4', 'GPS-004', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762b-811a-e67cc1384435', 0, 'OSBuildId5', 'GPS-005', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762c-8734-8ebafd5519dc', 1, 'OSBuildId6', 'GPS-006', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762d-99bd-db7a24384d1f', 3, 'OSBuildId7', 'GPS-007', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762e-bd5f-6cdd9df25a1f', 3, 'OSBuildId8', 'GPS-008', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-762f-9820-2957fc0a9d9d', 2, 'OSBuildId9', 'GPS-009', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7630-b5ed-f5a5bf948b6b', 0, 'OSBuildId11', 'Oppo 7', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7631-8ea3-cb3b022f209b', 0, 'OSBuildId12', 'GPS-011', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7632-9284-f17556880cf2', 0, 'OSBuildId13', 'GPS-012', NULL, NULL, false);
INSERT INTO public."GPSDevices" VALUES ('01968046-c456-7633-be7f-5d2d9fc344d7', 0, 'OSBuildId1', 'GPS-013', NULL, NULL, false);


--
-- Data for Name: CarContracts; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarGPSes; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarInspections; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarReports; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: CarStatistics; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Contracts; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: Feedbacks; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ImageTypes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."ImageTypes" VALUES ('01968046-c451-76a3-b4f5-cd299fd3ffe2', 'Car', NULL, NULL, false);
INSERT INTO public."ImageTypes" VALUES ('01968046-c451-76a4-b24e-8cd441ccf1d3', 'Paper', NULL, NULL, false);


--
-- Data for Name: ImageCars; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ImageFeedbacks; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: ImageReports; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: InspectionSchedules; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: InspectionPhotos; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: RefreshTokens; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: TransactionTypes; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a2-9d75-c77b96bc369c', 'BookingPayment', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a3-91b9-d4aceef4070b', 'ExtensionPayment', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a4-834c-5bcf7232b6d9', 'PlatformFee', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a5-9913-e4eb66b1ab2d', 'OwnerEarning', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a6-b91c-d1c0bb5f0447', 'Withdrawal', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a7-b752-1a7159d89b15', 'Refund', NULL, NULL, false);
INSERT INTO public."TransactionTypes" VALUES ('01968046-c452-71a8-ae3e-1e9fa3e4ab52', 'Compensation', NULL, NULL, false);


--
-- Data for Name: Transactions; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: TripTrackings; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: UserStatistics; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: WithdrawalRequests; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Data for Name: spatial_ref_sys; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- Name: aggregatedcounter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.aggregatedcounter_id_seq', 1, false);


--
-- Name: counter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.counter_id_seq', 1, false);


--
-- Name: hash_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.hash_id_seq', 35, true);


--
-- Name: job_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.job_id_seq', 1, false);


--
-- Name: jobparameter_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobparameter_id_seq', 1, false);


--
-- Name: jobqueue_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.jobqueue_id_seq', 1, false);


--
-- Name: list_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.list_id_seq', 1, false);


--
-- Name: set_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.set_id_seq', 5, true);


--
-- Name: state_id_seq; Type: SEQUENCE SET; Schema: hangfire; Owner: postgres
--

SELECT pg_catalog.setval('hangfire.state_id_seq', 1, false);


--
-- PostgreSQL database dump complete
--

