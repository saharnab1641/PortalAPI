# PortalAPI

## Route Definitions

**/api/Portal/RegisterUser**

- **Arguments** \- email(string), name(string), address(string)
- **Method** \- Post
- **Function** \- Sends email containing access key to the user

**/api/Portal/LoginUser**

- **Arguments** \- email(string), accessKey(string)
- **Method** \- Post
- **Function** \- Returns jwt token if user login successful

**/api/Portal/ForgotPassword**

- **Arguments** \- email(string)
- **Method** \- Post
- **Function** \- Sends email containing reset link

**/api/Portal/ResetPassword**

- **Arguments** \- token(string)(from the query string in password reset link)
- **Method** \- Post
- **Function** \- Sends email containing new access key to the user

**/api/Portal/PostNotification (~~Authorized Admin~~)**

- **Arguments** \- exam(string containing exam id), dayValidity(string)(defines the number of days for which the notification is valid), message(string)
- **Method** \- Post
- **Function** \- Adds a notification regarding a certain exam to database

**/api/Portal/GetNotification (Authorized)**

- **Arguments** \- none
- **Method** \- Get
- **Function** \- Gets all notifications relevant to the courses the user has enrolled in 

**/api/Portal/InitiatePayment (Authorized)** *(Optional route for added security)*

- **Arguments** \- exam(string containing exam id)
- **Method** \- Post
- **Function** \- Call this route before starting payment process to ensure the user hasn't already paid for the course 

**/api/Portal/ConfirmPayment (Authorized)**

- **Arguments** \- exam(string containing exam id), billing(string containing entire billing address extracted from stripe returned json and appended together), amount(string), token(string)
- **Method** \- Post
- **Function** \- Adds exam to the user's list of enrolled exams

**/api/Portal/ConfirmPayment (Authorized)**

- **Arguments** \- none
- **Method** \- Get
- **Function** \- Returns a list of purchases made by the user
