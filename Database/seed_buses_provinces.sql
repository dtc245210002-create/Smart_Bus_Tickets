-- =========================================================================
-- SCRIPT THÊM ĐA DẠNG BIỂN SỐ XE CHO CÁC TỈNH THÀNH TRÊN TOÀN QUỐC
-- =========================================================================

-- Bảng ánh xạ mã BusType:
-- BusTypeId 1: Xe ghế ngồi 16 chỗ (Capacity: 16)
-- BusTypeId 2: Xe ghế ngồi 29 chỗ (Capacity: 29)
-- BusTypeId 3: Xe ghế ngồi 45 chỗ (Capacity: 45)
-- BusTypeId 4: Limousine VIP 22 Phòng (Capacity: 22)
-- BusTypeId 5: Giường Nằm 34 Phòng (Capacity: 34)

DECLARE @BusesToAdd TABLE (
    BusTypeId INT,
    LicensePlate VARCHAR(20),
    Capacity INT,
    Status VARCHAR(20)
);

INSERT INTO @BusesToAdd (BusTypeId, LicensePlate, Capacity, Status) VALUES
-- 1. Thái Bình (Biển 17)
(2, '17B-012.34', 29, 'Active'),
(3, '17B-028.68', 45, 'Active'),
(4, '17B-003.52', 22, 'Active'),
(5, '17B-999.88', 34, 'Active'),
(2, '17F-001.68', 29, 'Active'),

-- 2. Hải Phòng (Biển 15, 16)
(2, '15B-123.45', 29, 'Active'),
(3, '15B-018.99', 45, 'Active'),
(4, '15B-026.88', 22, 'Active'),
(5, '16B-055.66', 34, 'Active'),
(4, '15F-002.34', 22, 'Active'),

-- 3. Hà Nội (Biển 29, 30)
(2, '29B-015.88', 29, 'Active'),
(3, '30B-088.99', 45, 'Active'),
(4, '30F-567.89', 22, 'Active'),
(5, '29B-999.11', 34, 'Active'),
(4, '29B-888.66', 22, 'Active'),

-- 4. Nam Định (Biển 18)
(2, '18B-015.67', 29, 'Active'),
(3, '18B-088.99', 45, 'Active'),
(4, '18B-002.34', 22, 'Active'),
(5, '18B-035.79', 34, 'Active'),

-- 5. Hải Dương (Biển 34)
(2, '34B-019.88', 29, 'Active'),
(3, '34B-023.45', 45, 'Active'),
(4, '34B-005.67', 22, 'Active'),
(5, '34B-068.86', 34, 'Active'),

-- 6. Hưng Yên (Biển 89)
(2, '89B-012.88', 29, 'Active'),
(3, '89B-035.79', 45, 'Active'),
(4, '89B-008.99', 22, 'Active'),
(5, '89B-099.66', 34, 'Active'),

-- 7. Quảng Ninh (Biển 14)
(2, '14B-019.99', 29, 'Active'),
(3, '14B-035.68', 45, 'Active'),
(4, '14B-028.88', 22, 'Active'),
(5, '14B-007.89', 34, 'Active'),

-- 8. Bắc Ninh (Biển 99)
(2, '99B-018.88', 29, 'Active'),
(3, '99B-022.33', 45, 'Active'),
(4, '99B-029.99', 22, 'Active'),
(5, '99B-088.77', 34, 'Active'),

-- 9. Bắc Giang (Biển 98)
(2, '98B-015.68', 29, 'Active'),
(3, '98B-033.44', 45, 'Active'),
(4, '98B-028.99', 22, 'Active'),
(5, '98B-077.88', 34, 'Active'),

-- 10. Thái Nguyên (Biển 20)
(3, '20B-023.45', 45, 'Active'),
(4, '20B-009.99', 22, 'Active'),
(5, '20B-088.66', 34, 'Active'),

-- 11. Vĩnh Phúc (Biển 88)
(2, '88B-012.34', 29, 'Active'),
(3, '88B-028.99', 45, 'Active'),
(4, '88B-006.78', 22, 'Active'),
(5, '88B-099.55', 34, 'Active'),

-- 12. Phú Thọ (Biển 19)
(2, '19B-015.68', 29, 'Active'),
(3, '19B-028.99', 45, 'Active'),
(4, '19B-007.89', 22, 'Active'),
(5, '19B-066.88', 34, 'Active'),

-- 13. Hà Nam (Biển 90)
(2, '90B-012.88', 29, 'Active'),
(3, '90B-025.79', 45, 'Active'),
(4, '90B-003.45', 22, 'Active'),
(5, '90B-088.99', 34, 'Active'),

-- 14. Ninh Bình (Biển 35)
(2, '35B-018.88', 29, 'Active'),
(3, '35B-033.66', 45, 'Active'),
(4, '35B-028.99', 22, 'Active'),
(5, '35B-099.77', 34, 'Active'),

-- 15. Thanh Hóa (Biển 36)
(2, '36B-025.68', 29, 'Active'),
(3, '36B-038.99', 45, 'Active'),
(4, '36B-006.78', 22, 'Active'),
(5, '36B-088.66', 34, 'Active'),

-- 16. Nghệ An (Biển 37)
(2, '37B-015.68', 29, 'Active'),
(3, '37B-019.88', 45, 'Active'),
(4, '37B-028.34', 22, 'Active'),
(5, '37B-099.99', 34, 'Active'),

-- 17. Hà Tĩnh (Biển 38)
(2, '38B-012.34', 29, 'Active'),
(3, '38B-016.78', 45, 'Active'),
(4, '38B-022.99', 22, 'Active'),
(5, '38B-088.55', 34, 'Active'),

-- 18. Quảng Bình (Biển 73)
(2, '73B-012.88', 29, 'Active'),
(3, '73B-025.68', 45, 'Active'),
(4, '73B-019.88', 22, 'Active'),
(5, '73B-099.77', 34, 'Active'),

-- 19. Quảng Trị (Biển 74)
(2, '74B-015.68', 29, 'Active'),
(3, '74B-028.99', 45, 'Active'),
(4, '74B-008.88', 22, 'Active'),
(5, '74B-077.66', 34, 'Active'),

-- 20. Thừa Thiên Huế (Biển 75)
(2, '75B-014.56', 29, 'Active'),
(3, '75B-022.99', 45, 'Active'),
(4, '75B-028.88', 22, 'Active'),
(5, '75B-033.22', 34, 'Active'),

-- 21. Đà Nẵng (Biển 43)
(2, '43B-019.23', 29, 'Active'),
(3, '43B-025.88', 45, 'Active'),
(4, '43B-028.88', 22, 'Active'),
(5, '43B-001.99', 34, 'Active'),

-- 22. Quảng Nam (Biển 92)
(2, '92B-015.67', 29, 'Active'),
(3, '92B-028.99', 45, 'Active'),
(4, '92B-006.88', 22, 'Active'),
(5, '92B-088.77', 34, 'Active'),

-- 23. Quảng Ngãi (Biển 76)
(2, '76B-012.34', 29, 'Active'),
(3, '76B-028.99', 45, 'Active'),
(4, '76B-009.88', 22, 'Active'),
(5, '76B-077.55', 34, 'Active'),

-- 24. Bình Định / Quy Nhơn (Biển 77)
(2, '77B-015.68', 29, 'Active'),
(3, '77B-028.99', 45, 'Active'),
(4, '77B-018.88', 22, 'Active'),
(5, '77B-035.99', 34, 'Active'),

-- 25. Phú Yên (Biển 78)
(2, '78B-012.88', 29, 'Active'),
(3, '78B-025.79', 45, 'Active'),
(4, '78B-008.99', 22, 'Active'),
(5, '78B-066.88', 34, 'Active'),

-- 26. Khánh Hòa / Nha Trang (Biển 79)
(2, '79B-025.68', 29, 'Active'),
(3, '79B-033.44', 45, 'Active'),
(4, '79B-038.99', 22, 'Active'),
(5, '79B-008.88', 34, 'Active'),

-- 27. Lâm Đồng / Đà Lạt (Biển 49)
(2, '49B-018.68', 29, 'Active'),
(3, '49B-022.33', 45, 'Active'),
(4, '49B-029.99', 22, 'Active'),
(5, '49B-003.45', 34, 'Active'),

-- 28. Đắk Lắk / Buôn Ma Thuột (Biển 47)
(2, '47B-012.34', 29, 'Active'),
(3, '47B-016.88', 45, 'Active'),
(4, '47B-028.99', 22, 'Active'),
(5, '47B-035.79', 34, 'Active'),

-- 29. Gia Lai (Biển 81)
(2, '81B-015.68', 29, 'Active'),
(3, '81B-028.99', 45, 'Active'),
(4, '81B-007.89', 22, 'Active'),
(5, '81B-088.66', 34, 'Active'),

-- 30. Kon Tum (Biển 82)
(2, '82B-012.88', 29, 'Active'),
(3, '82B-025.79', 45, 'Active'),
(4, '82B-009.99', 22, 'Active'),
(5, '82B-066.77', 34, 'Active'),

-- 31. Bình Thuận / Phan Thiết (Biển 86)
(2, '86B-015.78', 29, 'Active'),
(3, '86B-028.99', 45, 'Active'),
(4, '86B-006.88', 22, 'Active'),
(5, '86B-088.55', 34, 'Active'),

-- 32. Ninh Thuận (Biển 85)
(2, '85B-012.34', 29, 'Active'),
(3, '85B-028.99', 45, 'Active'),
(4, '85B-008.99', 22, 'Active'),
(5, '85B-077.66', 34, 'Active'),

-- 33. Bà Rịa - Vũng Tàu (Biển 72)
(2, '72B-018.99', 29, 'Active'),
(3, '72B-025.68', 45, 'Active'),
(4, '72B-029.88', 22, 'Active'),
(5, '72B-088.66', 34, 'Active'),

-- 34. Bình Dương (Biển 61)
(2, '61B-015.68', 29, 'Active'),
(3, '61B-028.99', 45, 'Active'),
(4, '61B-007.89', 22, 'Active'),
(5, '61B-099.55', 34, 'Active'),

-- 35. Đồng Nai (Biển 60, 39)
(2, '60B-019.88', 29, 'Active'),
(3, '60B-038.99', 45, 'Active'),
(4, '60B-008.99', 22, 'Active'),
(5, '60B-077.66', 34, 'Active'),

-- 36. Tây Ninh (Biển 70)
(2, '70B-015.68', 29, 'Active'),
(3, '70B-026.88', 45, 'Active'),
(4, '70B-009.99', 22, 'Active'),
(5, '70B-088.55', 34, 'Active'),

-- 37. TP. Hồ Chí Minh (Biển 50, 51)
(2, '50B-012.34', 29, 'Active'),
(3, '50B-016.78', 45, 'Active'),
(4, '51B-668.99', 22, 'Active'),
(5, '50F-006.88', 34, 'Active'),
(4, '51F-001.23', 22, 'Active'),

-- 38. Long An (Biển 62)
(2, '62B-015.68', 29, 'Active'),
(3, '62B-028.99', 45, 'Active'),
(4, '62B-006.88', 22, 'Active'),
(5, '62B-088.77', 34, 'Active'),

-- 39. Tiền Giang (Biển 63)
(2, '63B-018.99', 29, 'Active'),
(3, '63B-029.88', 45, 'Active'),
(4, '63B-007.89', 22, 'Active'),
(5, '63B-066.55', 34, 'Active'),

-- 40. Bến Tre (Biển 71)
(2, '71B-016.88', 29, 'Active'),
(3, '71B-025.79', 45, 'Active'),
(4, '71B-008.99', 22, 'Active'),
(5, '71B-088.66', 34, 'Active'),

-- 41. Cần Thơ (Biển 65)
(2, '65B-016.88', 29, 'Active'),
(3, '65B-022.33', 45, 'Active'),
(4, '65B-028.99', 22, 'Active'),
(5, '65B-001.55', 34, 'Active'),

-- 42. Vĩnh Long (Biển 64)
(2, '64B-012.34', 29, 'Active'),
(3, '64B-028.99', 45, 'Active'),
(4, '64B-009.88', 22, 'Active'),
(5, '64B-077.55', 34, 'Active'),

-- 43. Đồng Tháp (Biển 66)
(2, '66B-015.68', 29, 'Active'),
(3, '66B-028.99', 45, 'Active'),
(4, '66B-006.78', 22, 'Active'),
(5, '66B-088.66', 34, 'Active'),

-- 44. An Giang (Biển 67)
(2, '67B-012.88', 29, 'Active'),
(3, '67B-015.68', 45, 'Active'),
(4, '67B-009.99', 22, 'Active'),
(5, '67B-028.99', 34, 'Active'),

-- 45. Kiên Giang (Biển 68)
(2, '68B-018.88', 29, 'Active'),
(3, '68B-022.33', 45, 'Active'),
(4, '68B-008.99', 22, 'Active'),
(5, '68B-029.99', 34, 'Active'),

-- 46. Cà Mau (Biển 69)
(2, '69B-012.34', 29, 'Active'),
(3, '69B-018.88', 45, 'Active'),
(4, '69B-007.89', 22, 'Active'),
(5, '69B-029.99', 34, 'Active'),

-- 47. Lào Cai / Sa Pa (Biển 24)
(2, '24B-012.34', 29, 'Active'),
(3, '24B-025.79', 45, 'Active'),
(4, '24B-018.88', 22, 'Active'),
(5, '24B-022.33', 34, 'Active'),

-- 48. Hà Giang (Biển 23)
(2, '23B-012.88', 29, 'Active'),
(3, '23B-025.68', 45, 'Active'),
(4, '23B-015.68', 22, 'Active'),
(5, '23B-028.99', 34, 'Active'),

-- 49. Sơn La / Mộc Châu (Biển 26)
(2, '26B-012.34', 29, 'Active'),
(3, '26B-028.99', 45, 'Active'),
(4, '26B-016.88', 22, 'Active'),
(5, '26B-035.79', 34, 'Active'),

-- 50. Cao Bằng (Biển 11)
(2, '11B-012.88', 29, 'Active'),
(3, '11B-025.79', 45, 'Active'),
(4, '11B-018.99', 22, 'Active'),
(5, '11B-025.88', 34, 'Active');

-- Thêm vào bảng Bus nếu biển số chưa tồn tại
INSERT INTO [dbo].[Bus] ([BusTypeId], [LicensePlate], [Capacity], [Status])
SELECT src.BusTypeId, src.LicensePlate, src.Capacity, src.Status
FROM @BusesToAdd src
WHERE NOT EXISTS (
    SELECT 1 FROM [dbo].[Bus] b WHERE b.LicensePlate = src.LicensePlate
);

SELECT COUNT(*) AS TotalBuses FROM [dbo].[Bus];
