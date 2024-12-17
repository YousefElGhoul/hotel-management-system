open System
open System.Data
open Microsoft.Data.SqlClient

// تعريف connectionString
let connectionString = "Server=DESKTOP-780U7O9;Database=hotel;Trusted_Connection=True;TrustServerCertificate=True;"

// اختبار الاتصال بقاعدة البيانات
let testDatabaseConnection () =
    let mutable connection = new SqlConnection(connectionString)
    try
        connection.Open()
        printfn "Connection to the database is successful!"
    with
    | ex -> printfn "Database connection error: %s" ex.Message
    connection.Close()

// تنفيذ الاستعلامات
let executeQuery (query: string) (parameters: (string * obj) list) =
    let mutable connection = new SqlConnection(connectionString)
    try
        connection.Open()
        let mutable command = new SqlCommand(query, connection)
        for (name, value) in parameters do
            command.Parameters.AddWithValue(name, value) |> ignore
        command.ExecuteNonQuery() |> ignore
    with
    | ex -> printfn "Error executing query: %s" ex.Message
    connection.Close()

// جلب البيانات
let fetchData (query: string) (parameters: (string * obj) list) =
    let mutable connection = new SqlConnection(connectionString)
    let mutable results = ResizeArray<obj list>()
    try
        connection.Open()
        let mutable command = new SqlCommand(query, connection)
        for (name, value) in parameters do
            command.Parameters.AddWithValue(name, value) |> ignore
        let mutable reader = command.ExecuteReader()
        while reader.Read() do
            let mutable row = ResizeArray<obj>()
            for i = 0 to reader.FieldCount - 1 do
                row.Add(reader.GetValue(i))
            results.Add(row |> List.ofSeq)
    with
    | ex -> printfn "Error fetching data: %s" ex.Message
    connection.Close()
    results |> List.ofSeq

// إدارة الغرف
let addRoom (roomNumber: int) (roomType: string) (price: decimal) =
    let mutable query = "INSERT INTO Room (RoomNumber, RoomType, Price, Availability) VALUES (@RoomNumber, @RoomType, @Price, @Availability)"
    executeQuery query 
        [ "@RoomNumber", box roomNumber
          "@RoomType", box roomType
          "@Price", box price
          "@Availability", box 1 ] // Availability = 1 (الغرفة متاحة)
    printfn "Room added successfully: RoomNumber=%d, RoomType=%s" roomNumber roomType

let updateRoom (roomNumber: int) (newRoomType: string) (newPrice: decimal) =
    let mutable query = "UPDATE Room SET RoomType = @RoomType, Price = @Price WHERE RoomNumber = @RoomNumber"
    executeQuery query
        [ "@RoomNumber", box roomNumber
          "@RoomType", box newRoomType
          "@Price", box newPrice ]
    printfn "Room %d updated successfully." roomNumber

let checkRoomStatus (roomNumber: int) =
    let mutable query = "SELECT Availability FROM Room WHERE RoomNumber = @RoomNumber"
    let mutable result = fetchData query [ "@RoomNumber", box roomNumber ]
    let mutable status = false
    if List.length result = 1 && List.length result.[0] = 1 then
        match result.[0].[0] with
        | :? bool as availability -> 
            if availability then
                printfn "The room is available for booking."
                status <- true
            else
                printfn "The room is already booked."
                status <- false
        | _ -> printfn "Error: Invalid data type for availability."; status <- false
    else
        printfn "Error: Query result structure is incorrect."
        status <- false
    status

let deleteRoom (roomNumber: int) =
    let mutable cancelQuery = "DELETE FROM Reservations WHERE RoomNumber = @RoomNumber"
    let mutable deleteQuery = "DELETE FROM Room WHERE RoomNumber = @RoomNumber"
    executeQuery cancelQuery [ "@RoomNumber", box roomNumber ]
    executeQuery deleteQuery [ "@RoomNumber", box roomNumber ]
    printfn "Room %d has been deleted." roomNumber

let bookRoom (customerId: int) (roomNumber: int) (startDate: DateTime) (endDate: DateTime) (promotionId: int) =
    let mutable reserveQuery = 
        "INSERT INTO Reservations (customerId, RoomNumber, StartDate, EndDate, promotionId) VALUES (@customerId, @RoomNumber, @StartDate, @EndDate, @promotionId)"
    let mutable updateRoomQuery = 
        "UPDATE Room SET Availability = 0 WHERE RoomNumber = @RoomNumber"
    try
        executeQuery reserveQuery
            [ "@customerId", box customerId
              "@RoomNumber", box roomNumber
              "@StartDate", box startDate
              "@EndDate", box endDate
              "@promotionId", box promotionId ]
        executeQuery updateRoomQuery [ "@RoomNumber", box roomNumber ]
        printfn "Room %d successfully reserved for customer %d." roomNumber customerId
    with
    | ex -> printfn "Error while booking room: %s" ex.Message

// إدارة العملاء
let addCustomer (customerName: string) (contactInfo: string) (paymentMethod: string) =
    let mutable query = "INSERT INTO Customers (CustomerName, ContactInfo, PaymentMethod) VALUES (@CustomerName, @ContactInfo, @PaymentMethod)"
    executeQuery query 
        [ "@CustomerName", box customerName
          "@ContactInfo", box contactInfo
          "@PaymentMethod", box paymentMethod ]
    printfn "Customer %s added successfully." customerName

let updateCustomer (customerId: int) (newName: string) (newContactInfo: string) (newPaymentMethod: string) =
    let mutable query = "UPDATE Customers SET CustomerName = @CustomerName, ContactInfo = @ContactInfo, PaymentMethod = @PaymentMethod WHERE CustomerId = @CustomerId"
    executeQuery query
        [ "@CustomerId", box customerId
          "@CustomerName", box newName
          "@ContactInfo", box newContactInfo
          "@PaymentMethod", box newPaymentMethod ]
    printfn "Customer %d updated successfully." customerId

let searchCustomer (searchTerm: string) =
    let mutable query = "SELECT CustomerId, CustomerName, ContactInfo, PaymentMethod FROM Customers WHERE CustomerName LIKE @SearchTerm OR ContactInfo LIKE @SearchTerm"
    let mutable parameters = [ "@SearchTerm", box ("%" + searchTerm + "%") ]
    try
        let mutable results = fetchData query parameters
        if List.length results > 0 then
            for row in results do
                printfn "CustomerId: %A, CustomerName: %A, ContactInfo: %A, PaymentMethod: %A" row.[0] row.[1] row.[2] row.[3]
        else
            printfn "No customers found matching the search term '%s'." searchTerm
    with
    | ex -> printfn "Error searching for customer: %s" ex.Message

// إدارة العروض الترويجية
let addPromotion (promotionName: string) (discountPercentage: decimal) (startDate: DateTime )(endDate:DateTime)=
    let mutable query = "INSERT INTO Promotions (PromotionName, DiscountPercentage,startDate, EndDate) VALUES (@PromotionName, @DiscountPercentage,@startDate, @EndDate)"
    executeQuery query
        [ "@PromotionName", box promotionName
          "@DiscountPercentage", box discountPercentage
          "@startDate", box startDate 
          "@EndDate", box endDate ]
    printfn "Promotion '%s' added successfully." promotionName

let updatePromotion (promotionId: int) (newName: string) (newDiscount: decimal) =
    let mutable query = "UPDATE Promotions SET PromotionName = @PromotionName, DiscountPercentage = @DiscountPercentage WHERE PromotionId = @PromotionId"
    executeQuery query
        [ "@PromotionId", box promotionId
          "@PromotionName", box newName
          "@DiscountPercentage", box newDiscount ]
    printfn "Promotion %d updated successfully." promotionId

let deletePromotion (promotionId: int) =
    let mutable query = "DELETE FROM Promotions WHERE PromotionId = @PromotionId"
    executeQuery query [ "@PromotionId", box promotionId ]
    printfn "Promotion %d deleted successfully." promotionId



// إدارة الخدمات
let addService (serviceName: string) (servicePrice: decimal) (roomNumber: int) =
    let mutable query = "INSERT INTO Services (ServiceName, ServicePrice, roomNumber) VALUES (@ServiceName, @ServicePrice, @roomNumber)"
    executeQuery query 
        [ 
            "@roomNumber", box roomNumber
            "@ServiceName", box serviceName
            "@ServicePrice", box servicePrice 
        ]
    printfn "Service added: %s at price %.2f" serviceName servicePrice

// دالة لحساب تفاصيل الفاتورة
let calculateBillDetails (reservationId: int) (roomNumber: int) (startDate: DateTime) (endDate: DateTime) (discount: decimal) (promotionId: int) =
    // استعلام للحصول على سعر الغرفة
    let query = "SELECT Price FROM Room WHERE RoomNumber = @RoomNumber"
    let result: obj list list = fetchData query [ "@RoomNumber", box roomNumber ]
    
    // التحقق من أن النتيجة صحيحة
    let mutable price = 0.0M
    if result.Length = 1 && result.[0].Length = 1 then
        match result.[0].[0] with
        | :? decimal as fetchedPrice -> price <- fetchedPrice
        | _ -> price <- 0.0M

    // حساب مدة الإقامة والضرائب
    let duration = (endDate - startDate).Days
    let taxes = price * 0.1M
    let totalBeforeDiscount = price * decimal duration + taxes

    // استعلام للحصول على تكلفة الخدمات الإضافية
    let servicesQuery = """
        SELECT SUM(ServicePrice * Quantity) 
        FROM Services s
        JOIN ReservationServices rs ON s.ServiceId = rs.ServiceId
        WHERE rs.ReservationId = @ReservationId
    """
    let servicesResult = fetchData servicesQuery [ "@ReservationId", box reservationId ]
    let mutable serviceCost = 0.0M
    if servicesResult.Length = 1 && servicesResult.[0].[0] :? decimal then
        serviceCost <- servicesResult.[0].[0] :?> decimal

    // حساب المبلغ بعد الخصم
    let totalAfterDiscount = totalBeforeDiscount - discount + serviceCost

    // إرجاع تفاصيل الفاتورة
    // Return Some tuple with all details (TotalBeforeDiscount, TotalAfterDiscount, Taxes, Discount)
    Some (totalBeforeDiscount, totalAfterDiscount, taxes, discount)

// دالة لحفظ الفاتورة في قاعدة البيانات
let saveBillToDatabase (reservationId: int) (totalAfterDiscount: decimal) (taxes: decimal) (discount: decimal) (paymentStatus: string) (promotionId: int) =
    let billQuery = """
        INSERT INTO Bill (ReservationId, TotalAmount, Taxes, Discount, PaymentStatus, PromotionId) 
        VALUES (@ReservationId, @TotalAmount, @Taxes, @Discount, @PaymentStatus, @PromotionId)
    """
    executeQuery billQuery
        [
            "@ReservationId", box reservationId
            "@TotalAmount", box totalAfterDiscount
            "@Taxes", box taxes
            "@Discount", box discount
            "@PaymentStatus", box paymentStatus
            "@PromotionId", box promotionId
        ]
    printfn "Bill saved to database successfully!"

// Main function to calculate and save the bill
let reservations reservationId roomNumber (startDate: DateTime) (endDate: DateTime) discount promotionId =
    // Declare mutable variables to store calculation results
    let mutable totalAfterDiscount = 0.0M
    let mutable taxes = 0.0M
    let mutable serviceCost = 0.0M
    let mutable totalDiscount = 0.0M
    
    // Get bill details by calling calculateBillDetails
    match calculateBillDetails reservationId roomNumber startDate endDate discount promotionId with
    | Some (calculatedTotalBeforeDiscount, calculatedTotalAfterDiscount, calculatedTaxes, calculatedDiscount) ->
        // Set the mutable variables with calculated values
        totalAfterDiscount <- calculatedTotalAfterDiscount
        taxes <- calculatedTaxes
        serviceCost <- calculatedTotalBeforeDiscount - calculatedTotalAfterDiscount
        totalDiscount <- calculatedDiscount

        // Call the function to save the bill to the database
        saveBillToDatabase reservationId totalAfterDiscount taxes totalDiscount "Unpaid" promotionId
        // Return the total amount after discount
        totalAfterDiscount
    | None -> 
        printfn "Error in bill calculation."
        0.0M



// دالة لحساب تفاصيل التقرير
let calculateReportDetails (reportType: string) (startDate: DateTime) (endDate: DateTime) =
    // استعلام لحساب إجمالي الإيرادات
    let mutable revenueQuery = """
        SELECT SUM(b.TotalAmount) 
        FROM Bill b
        JOIN Reservations r ON b.ReservationId = r.ReservationId
        WHERE r.StartDate >= @StartDate AND r.EndDate <= @EndDate
    """
    let mutable revenueResult = fetchData revenueQuery [ "@StartDate", box startDate; "@EndDate", box endDate ]
    let mutable totalRevenue = 0.0M
    if revenueResult.Length = 1 && revenueResult.[0].Length = 1 then
        match revenueResult.[0].[0] with
        | :? decimal as total -> totalRevenue <- total
        | _ -> totalRevenue <- 0.0M

    printfn "Total Revenue: %A" totalRevenue

    // استعلام لحساب إجمالي عدد الحجوزات
    let mutable reservationsQuery = """
        SELECT COUNT(*)
        FROM Reservations r
        WHERE r.StartDate >= @StartDate AND r.EndDate <= @EndDate
    """
    let mutable reservationsResult = fetchData reservationsQuery [ "@StartDate", box startDate; "@EndDate", box endDate ]
    let mutable totalReservations = 0
    if reservationsResult.Length = 1 && reservationsResult.[0].Length = 1 then
        match reservationsResult.[0].[0] with
        | :? int as count -> totalReservations <- count
        | _ -> totalReservations <- 0

    printfn "Total Reservations: %d" totalReservations

    // استعلام لحساب إجمالي الخصومات
    let mutable discountsQuery = """
        SELECT SUM(b.Discount)
        FROM Bill b
        JOIN Reservations r ON b.ReservationId = r.ReservationId
        WHERE r.StartDate >= @StartDate AND r.EndDate <= @EndDate
    """
    let mutable discountsResult = fetchData discountsQuery [ "@StartDate", box startDate; "@EndDate", box endDate ]
    let mutable totalDiscounts = 0.0M
    if discountsResult.Length = 1 && discountsResult.[0].Length = 1 then
        match discountsResult.[0].[0] with
        | :? decimal as totalDiscount -> totalDiscounts <- totalDiscount
        | _ -> totalDiscounts <- 0.0M

    printfn "Total Discounts: %A" totalDiscounts

    // استعلام لحساب معدل الإشغال
    let mutable occupancyQuery = """
        SELECT COUNT(*)
        FROM Room r
        WHERE r.Availability = 0
    """
    let mutable occupancyResult = fetchData occupancyQuery []
    let mutable occupiedRooms = 0
    if occupancyResult.Length = 1 && occupancyResult.[0].Length = 1 then
        match occupancyResult.[0].[0] with
        | :? int as occupied -> occupiedRooms <- occupied
        | _ -> occupiedRooms <- 0

    printfn "Occupied Rooms: %d" occupiedRooms

    // استعلام لحساب إجمالي الغرف المتاحة
    let mutable totalRoomsQuery = "SELECT COUNT(*) FROM Room"
    let mutable totalRoomsResult = fetchData totalRoomsQuery []
    let mutable totalRooms = 0
    if totalRoomsResult.Length = 1 && totalRoomsResult.[0].Length = 1 then
        match totalRoomsResult.[0].[0] with
        | :? int as total -> totalRooms <- total
        | _ -> totalRooms <- 0

    printfn "Total Rooms: %d" totalRooms

    // حساب معدل الإشغال
    let mutable occupancyRate = 0.0M
    if totalRooms > 0 then
        occupancyRate <- (decimal occupiedRooms / decimal totalRooms) * 100.0M

    printfn "Occupancy Rate: %A" occupancyRate

    // إرجاع القيم المحسوبة مع المعرفات
    (totalRevenue, totalReservations, occupancyRate, totalDiscounts, totalRooms, occupiedRooms)

// دالة لحفظ التقرير في قاعدة البيانات
let saveReportToDatabase (reportType: string) (totalRevenue: decimal) (totalReservations: int) (occupancyRate: decimal) (totalDiscounts: decimal) (promotionId: int) (billId: int) (reservationId: int) =
    let mutable insertReportQuery = """
        INSERT INTO Report (ReportType, GeneratedDate, TotalRevenue, TotalReservations, OccupancyRate, TotalDiscounts, PromotionId, BillId, ReservationId)
        VALUES (@ReportType, @GeneratedDate, @TotalRevenue, @TotalReservations, @OccupancyRate, @TotalDiscounts, @PromotionId, @BillId, @ReservationId)
    """
    executeQuery insertReportQuery 
        [
            "@ReportType", box reportType
            "@GeneratedDate", box DateTime.Now
            "@TotalRevenue", box totalRevenue
            "@TotalReservations", box totalReservations
            "@OccupancyRate", box occupancyRate
            "@TotalDiscounts", box totalDiscounts
            "@PromotionId", box promotionId
            "@BillId", box billId
            "@ReservationId", box reservationId
        ]
    printfn "Report saved to database successfully!"

// دالة لتوليد وحفظ التقرير مع المعرفات المطلوبة
let generateAndSaveReport (reportType: string) (startDate: DateTime) (endDate: DateTime) (promotionId: int) (billId: int) (reservationId: int) =
    // استدعاء الدالة لحساب تفاصيل التقرير
    let (totalRevenue, totalReservations, occupancyRate, totalDiscounts, totalRooms, occupiedRooms) = 
        calculateReportDetails reportType startDate endDate

    // استدعاء الدالة لحفظ التقرير في قاعدة البيانات مع المعرفات
    saveReportToDatabase reportType totalRevenue totalReservations occupancyRate totalDiscounts promotionId billId reservationId
    printfn "Report generated and saved successfully!"


    // Check in function
let checkIn reservationId =
    let query = "UPDATE Reservations SET CheckIn = 1 WHERE ReservationId = @ReservationId"
    executeQuery query [ "@ReservationId", box reservationId ]
    printfn "Checked in reservation %d." reservationId

// Check out function
let checkOut reservationId =
    let query = "UPDATE Reservations SET CheckOut = 1 WHERE ReservationId = @ReservationId"
    executeQuery query [ "@ReservationId", box reservationId ]
    printfn "Checked out reservation %d." reservationId

// Function to check and update check-out status based on date
let updateCheckOutStatus reservationId endDate =
    let mutable currentDate = DateTime.Now
    if currentDate >= endDate then
        checkOut reservationId
        printfn "Reservation %d has checked out as the end date has passed." reservationId
    else
        printfn "Reservation %d has not checked out yet." reservationId
[<EntryPoint>]
let main argv =
    // اختبار الاتصال بقاعدة البيانات
    testDatabaseConnection()

    // إضافة غرف
    printfn "Adding rooms..."
    addRoom 70"Single" 234567 

    // إضافة خدمات
    addService "Room Cleaning" 50.00M 70
    //addService "Breakfast" 30.00M

    // إضافة عروض ترويجية مع تاريخ الانتهاء
    addPromotion "Spring Discount" 20.00M (DateTime(2024, 5, 1))(DateTime(2024, 5, 30))
    //addPromotion "Holiday Special" 15.00M (DateTime(2025, 2, 3))(DateTime(2024, 12, 31))

    // إضافة عميل
    addCustomer "John Doe" "123456789" "Credit Card"

    // تحقق من حالة الغرفة 104
    let isAvailable = checkRoomStatus 104
    //bookRoom customerId roomNumber (startDate: DateTime) (endDate: DateTime) promotionId =
    // حجز غرفة
    let startDate = DateTime(2024, 5,1)
    let endDate = DateTime(2024, 5, 30)
    bookRoom  263 70 startDate endDate 193


    let reservationId = 54
    let roomNumber = 70
    let startDate = DateTime(2024, 5,1)
    let endDate = DateTime(2024, 5,30)
    let discount = 50.0M
    let promotionId = 193
    
    // Call reservations function to calculate and save the bill
    let totalAmount = reservations reservationId roomNumber startDate endDate discount promotionId
    
    // Print the result
    printfn "Total amount after discount: %M" totalAmount
    
    let reportType = "Monthly"  // نوع التقرير
   
    let promotionId = 193  // مثال: ID للترقية
    let billId = 42  // مثال: ID للفاتورة
    let reservationId = 54  // مثال: ID للحجز

    // استدعاء دالة توليد وحفظ التقرير
    generateAndSaveReport reportType startDate endDate promotionId billId reservationId
   
   // Simulate the check-in process
    checkIn reservationId

    // Simulate checking the status of check-out
    updateCheckOutStatus reservationId endDate

    // Simulate the check-out process (if the reservation date has passed)
    updateCheckOutStatus reservationId DateTime.Now
    ////// إخلاء غرفة
    //printfn "Deleting room..."
    //deleteRoom 102
    0



