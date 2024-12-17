open System
open System.Data
open Microsoft.Data.SqlClient

// Connection string (immutable)
let connectionString = 
    "Server=DESKTOP-780U7O9;Database=hotel;Trusted_Connection=True;TrustServerCertificate=True;"

// Helper function to add parameters recursively
let rec addParameters (command: SqlCommand) parameters =
    match parameters with
    | [] -> ()
    | (name, value) :: tail ->
        command.Parameters.AddWithValue(name, value) |> ignore
        addParameters command tail

// Recursive function to execute a query (INSERT/UPDATE/DELETE)
let rec executeQuery query parameters =
    use connection = new SqlConnection(connectionString)
    connection.Open()
    use command = new SqlCommand(query, connection)
    addParameters command parameters
    command.ExecuteNonQuery() |> ignore

// Recursive function to fetch data
let fetchData query parameters =
    use connection = new SqlConnection(connectionString)
    connection.Open()
    use command = new SqlCommand(query, connection)
    addParameters command parameters
    use reader = command.ExecuteReader()

    let rec readRows acc =
        if reader.Read() then
            let row = [ for i in 0 .. reader.FieldCount - 1 -> reader.GetValue(i) ]
            readRows (row :: acc)
        else List.rev acc
    readRows []

// Add a room (recursive approach)
let rec addRoom roomNumber roomType price =
    let query = 
        "INSERT INTO Room (RoomNumber, RoomType, Price, Availability) VALUES (@RoomNumber, @RoomType, @Price, @Availability)"
    executeQuery query 
        [ "@RoomNumber", box roomNumber
          "@RoomType", box roomType
          "@Price", box price
          "@Availability", box 1 ]
    printfn "Room %d of type '%s' added successfully." roomNumber roomType

// Update a room
let rec updateRoom roomNumber newRoomType newPrice =
    let query = 
        "UPDATE Room SET RoomType = @RoomType, Price = @Price WHERE RoomNumber = @RoomNumber"
    executeQuery query 
        [ "@RoomNumber", box roomNumber
          "@RoomType", box newRoomType
          "@Price", box newPrice ]
    printfn "Room %d updated successfully." roomNumber

// Check room status (recursion and pattern matching)
let checkRoomStatus (roomNumber: int) : bool =
    let query = "SELECT Availability FROM Room WHERE RoomNumber = @RoomNumber"
    match fetchData query [ "@RoomNumber", box roomNumber ] with
    | [[ :? int as availability ]] when availability = 1 -> 
        printfn "Room %d is available." roomNumber
        true
    | [[ :? int as _ ]] -> 
        printfn "Room %d is already booked." roomNumber
        false
    | _ -> 
        printfn "Room %d not found or error occurred." roomNumber
        false

// Delete a room
let rec deleteRoom roomNumber =
    let queries = 
        [ "DELETE FROM Reservations WHERE RoomNumber = @RoomNumber"
          "DELETE FROM Room WHERE RoomNumber = @RoomNumber" ]
    queries |> List.iter (fun query -> executeQuery query [ "@RoomNumber", box roomNumber ])
    printfn "Room %d has been deleted." roomNumber

// Book a room
let bookRoom (customerId: int) (roomNumber: int) (startDate: DateTime) (endDate: DateTime) (promotionId: int)  =
    let reserveQuery = 
        "INSERT INTO Reservations (CustomerId, RoomNumber, StartDate, EndDate, PromotionId) VALUES (@CustomerId, @RoomNumber, @StartDate, @EndDate, @PromotionId)"
    let updateQuery = "UPDATE Room SET Availability = 0 WHERE RoomNumber = @RoomNumber"
    try
        executeQuery reserveQuery 
            [ "@CustomerId", box customerId
              "@RoomNumber", box roomNumber
              "@StartDate", box startDate
              "@EndDate", box endDate
              "@PromotionId", box promotionId ]
        executeQuery updateQuery [ "@RoomNumber", box roomNumber ]
        printfn "Room %d successfully reserved for customer %d." roomNumber customerId
    with
    | ex -> printfn "Error while booking room: %s" ex.Message

// Add a customer
let rec addCustomer customerName contactInfo paymentMethod =
    let query = 
        "INSERT INTO Customers (CustomerName, ContactInfo, PaymentMethod) VALUES (@CustomerName, @ContactInfo, @PaymentMethod)"
    executeQuery query 
        [ "@CustomerName", box customerName
          "@ContactInfo", box contactInfo
          "@PaymentMethod", box paymentMethod ]
    printfn "Customer '%s' added successfully." customerName

// Search for a customer (recursive processing of results)
let rec searchCustomer searchTerm =
    let query = 
        "SELECT CustomerId, CustomerName, ContactInfo, PaymentMethod FROM Customers WHERE CustomerName LIKE @SearchTerm OR ContactInfo LIKE @SearchTerm"
    let results = fetchData query [ "@SearchTerm", box ("%" + searchTerm + "%") ]
    match results with
    | [] -> printfn "No customers found for search term: '%s'." searchTerm
    | _ -> 
        results |> List.iter (fun row ->
            printfn "CustomerId: %A, Name: %A, Contact: %A, Payment: %A" row.[0] row.[1] row.[2] row.[3])

// Update customer details function
let rec updateCustomer customerId newName newContactInfo newPaymentMethod =
    let query = 
        "UPDATE Customers SET CustomerName = @CustomerName, ContactInfo = @ContactInfo, PaymentMethod = @PaymentMethod WHERE CustomerId = @CustomerId"
    executeQuery query 
        [ "@CustomerId", box customerId
          "@CustomerName", box newName
          "@ContactInfo", box newContactInfo
          "@PaymentMethod", box newPaymentMethod ]
    printfn "Customer with ID %d updated successfully." customerId

let rec addPromotion promotionName discountPercentage startDate endDate =
    let query = """
        INSERT INTO Promotions (PromotionName, DiscountPercentage, startDate, EndDate)
        VALUES (@PromotionName, @DiscountPercentage, @startDate, @EndDate)
    """
    executeQuery query
        [ "@PromotionName", box promotionName
          "@DiscountPercentage", box discountPercentage
          "@startDate", box startDate
          "@EndDate", box endDate ]
    printfn "Promotion '%s' added successfully." promotionName

let rec updatePromotion promotionId newName newDiscount =
    let query = """
        UPDATE Promotions 
        SET PromotionName = @PromotionName, DiscountPercentage = @DiscountPercentage 
        WHERE PromotionId = @PromotionId
    """
    executeQuery query
        [ "@PromotionId", box promotionId
          "@PromotionName", box newName
          "@DiscountPercentage", box newDiscount ]
    printfn "Promotion %d updated successfully." promotionId

let rec deletePromotion promotionId =
    let query = "DELETE FROM Promotions WHERE PromotionId = @PromotionId"
    executeQuery query [ "@PromotionId", box promotionId ]
    printfn "Promotion %d deleted successfully." promotionId

let rec addService serviceName servicePrice roomNumber =
    let query = """
        INSERT INTO Services (ServiceName, ServicePrice, roomNumber)
        VALUES (@ServiceName, @ServicePrice, @roomNumber)
    """
    executeQuery query
        [ "@ServiceName", box serviceName
          "@ServicePrice", box servicePrice
          "@roomNumber", box roomNumber ]
    printfn "Service '%s' added successfully at price %.2f for room %d." serviceName servicePrice roomNumber

open System

// Function to calculate bill details
let rec calculateBillDetails reservationId roomNumber (startDate: DateTime) (endDate: DateTime) discount promotionId =
    // Query to fetch the room price
    let query = "SELECT Price FROM Room WHERE RoomNumber = @RoomNumber"
    let result = fetchData query [ "@RoomNumber", box roomNumber ]

    // Extract the price using pattern matching
    let price =
        match result with
        | [[ :? decimal as fetchedPrice ]] -> fetchedPrice
        | _ ->
            printfn "Error fetching room price. Defaulting to 0."
            0.0M

    // Calculate duration and taxes
    let duration = (endDate - startDate).Days
    let taxes = price * 0.1M
    let totalBeforeDiscount = price * decimal duration + taxes

    // Query to fetch additional service costs
    let servicesQuery = """
        SELECT SUM(ServicePrice * Quantity)
        FROM Services s
        JOIN ReservationServices rs ON s.ServiceId = rs.ServiceId
        WHERE rs.ReservationId = @ReservationId
    """
    let servicesResult = fetchData servicesQuery [ "@ReservationId", box reservationId ]
    let serviceCost =
        match servicesResult with
        | [[ :? decimal as fetchedCost ]] -> fetchedCost
        | _ -> 0.0M

    // Calculate final total after discount
    let totalAfterDiscount = totalBeforeDiscount - discount + serviceCost

    // Return bill details as an immutable tuple
    Some (totalBeforeDiscount, totalAfterDiscount, taxes, discount)

// Function to save bill details to the database
let saveBillToDatabase reservationId totalAfterDiscount taxes discount paymentStatus promotionId =
    let query = """
        INSERT INTO Bill (ReservationId, TotalAmount, Taxes, Discount, PaymentStatus, PromotionId)
        VALUES (@ReservationId, @TotalAmount, @Taxes, @Discount, @PaymentStatus, @PromotionId)
    """
    executeQuery query
        [
            "@ReservationId", box reservationId
            "@TotalAmount", box totalAfterDiscount
            "@Taxes", box taxes
            "@Discount", box discount
            "@PaymentStatus", box paymentStatus
            "@PromotionId", box promotionId
        ]
    printfn "Bill saved to database successfully."

// Function to calculate and save bill
let rec processReservation reservationId roomNumber startDate endDate discount promotionId =
    match calculateBillDetails reservationId roomNumber startDate endDate discount promotionId with
    | Some (totalBeforeDiscount, totalAfterDiscount, taxes, discountAmount) ->
        saveBillToDatabase reservationId totalAfterDiscount taxes discountAmount "Unpaid" promotionId
        printfn "Total amount after discount: %.2f" totalAfterDiscount
        totalAfterDiscount
    | None ->
        printfn "Failed to calculate bill details."
        0.0M

open System

// Function to calculate report details
let  calculateReportDetails reportType (startDate: DateTime) (endDate: DateTime) =
    // Query to calculate total revenue
    let revenueQuery = """
        SELECT SUM(b.TotalAmount) 
        FROM Bill b
        JOIN Reservations r ON b.ReservationId = r.ReservationId
        WHERE r.StartDate >= @StartDate AND r.EndDate <= @EndDate
    """
    let revenueResult = fetchData revenueQuery [ "@StartDate", box startDate; "@EndDate", box endDate ]

    let totalRevenue =
        match revenueResult with
        | [[ :? decimal as revenue ]] -> revenue
        | _ -> 0.0M

    printfn "Total Revenue: %A" totalRevenue

    // Query to calculate total reservations
    let reservationsQuery = """
        SELECT COUNT(*)
        FROM Reservations
        WHERE StartDate >= @StartDate AND EndDate <= @EndDate
    """
    let reservationsResult = fetchData reservationsQuery [ "@StartDate", box startDate; "@EndDate", box endDate ]

    let totalReservations =
        match reservationsResult with
        | [[ :? int as count ]] -> count
        | _ -> 0

    printfn "Total Reservations: %d" totalReservations

    // Query to calculate occupied rooms
    let occupancyQuery = """
        SELECT COUNT(*)
        FROM Room
        WHERE Availability = 0
    """
    let occupancyResult = fetchData occupancyQuery []

    let occupiedRooms =
        match occupancyResult with
        | [[ :? int as count ]] -> count
        | _ -> 0

    printfn "Occupied Rooms: %d" occupiedRooms

    // Query to calculate total rooms
    let totalRoomsQuery = "SELECT COUNT(*) FROM Room"
    let totalRoomsResult = fetchData totalRoomsQuery []

    let totalRooms =
        match totalRoomsResult with
        | [[ :? int as count ]] -> count
        | _ -> 0

    printfn "Total Rooms: %d" totalRooms

    // Calculate occupancy rate
    let occupancyRate =
        if totalRooms > 0 then
            (decimal occupiedRooms / decimal totalRooms) * 100.0M
        else
            0.0M

    printfn "Occupancy Rate: %A%%" occupancyRate

    // Return the calculated report details
    (totalRevenue, totalReservations, occupancyRate)

// Function to save the report to the database
let saveReportToDatabase reportType totalRevenue totalReservations occupancyRate promotionId billId reservationId =
    let insertQuery = """
        INSERT INTO Report (ReportType, GeneratedDate, TotalRevenue, TotalReservations, OccupancyRate, PromotionId, BillId, ReservationId)
        VALUES (@ReportType, @GeneratedDate, @TotalRevenue, @TotalReservations, @OccupancyRate, @PromotionId, @BillId, @ReservationId)
    """
    executeQuery insertQuery 
        [
            "@ReportType", box reportType
            "@GeneratedDate", box DateTime.Now
            "@TotalRevenue", box totalRevenue
            "@TotalReservations", box totalReservations
            "@OccupancyRate", box occupancyRate
            "@PromotionId", box promotionId
            "@BillId", box billId
            "@ReservationId", box reservationId
        ]
    printfn "Report saved to database successfully."

// Function to generate and save the report
let generateAndSaveReport reportType startDate endDate promotionId billId reservationId =
    // Calculate the report details
    let (totalRevenue, totalReservations, occupancyRate) = calculateReportDetails reportType startDate endDate

    // Save the report to the database
    saveReportToDatabase reportType totalRevenue totalReservations occupancyRate promotionId billId reservationId

    printfn "Report generated and saved successfully."

// Function for Check-In
let checkIn reservationId =
    let query = "UPDATE Reservations SET CheckIn = 1 WHERE ReservationId = @ReservationId"
    executeQuery query [ "@ReservationId", box reservationId ]
    printfn "Checked in reservation %d." reservationId

// Function for Check-Out
let checkOut reservationId =
    let query = "UPDATE Reservations SET CheckOut = 1 WHERE ReservationId = @ReservationId"
    executeQuery query [ "@ReservationId", box reservationId ]
    printfn "Checked out reservation %d." reservationId

// Function to update check-out status
let updateCheckOutStatus reservationId (endDate: DateTime) =
    let currentDate = DateTime.Now
    if currentDate >= endDate then
        checkOut reservationId
        printfn "Reservation %d has checked out as the end date has passed." reservationId
    else
        printfn "Reservation %d has not checked out yet." reservationId

// Main function to demonstrate functionality
[<EntryPoint>]
let main _ =
    // Adding and managing rooms
    addRoom 101 "Deluxe" 150.0
    addRoom 102 "Suite" 300.0
    updateRoom 101 "Super Deluxe" 200.0
    bookRoom 276 101 (DateTime(2024, 4, 1)) (DateTime(2024, 4, 30)) 202
    //let isAvailable = checkRoomStatus 101
    //if isAvailable then 
    //  bookRoom 276 101 (DateTime(2024, 4, 1)) (DateTime(2024, 4, 30)) 202
    //else 
    // () 
    // Adding and searching for customers
    addCustomer "John Doe" "123456789" "Credit Card"
    searchCustomer "John"
    updateCustomer 1 "Jane Doe" "987654321" "Debit Card"
   
    // Deleting a room
    deleteRoom 102

    // Adding a new promotion
    addPromotion "Spring Offer" 20.0M (DateTime(2024, 4, 1)) (DateTime(2024, 4, 30))
    
    // Updating a promotion
    updatePromotion 1 "Spring Sale Extended" 25.0M 
    
    // Deleting a promotion
    deletePromotion 2
    
    // Adding a service to a room
    addService "Room Cleaning" 50.0M 101
    addService "Mini Bar" 100.0M 102
    
    // Example reservation details
    let reservationId = 64
    let roomNumber = 101
    let startDate = DateTime(2024, 4, 1)
    let endDate = DateTime(2024, 4, 30)
    let discount = 50.0M
    let promotionId = 203

    // Process the reservation
    processReservation reservationId roomNumber startDate endDate discount promotionId

    // Generate and save the report
    let reportType = "Monthly Report"
    let billId = 101
    let reportReservationId = 64

    generateAndSaveReport reportType startDate endDate promotionId billId reportReservationId

    // Example of check-in and check-out process
    checkIn reservationId
    updateCheckOutStatus reservationId endDate

    printfn "Hotel management operations completed successfully."
    0 // Exit code



