# RMT Business Website

A personal business website developed for a Registered Massage Therapist using **C# and .NET 10 Blazor**. The application provides visitors with information about available massage therapy services, practitioner background, frequently asked questions, and a contact form for submitting inquiries.

## Project Overview

The application was developed as a practical full-stack web development project to build a professional service-based website using Microsoft's Blazor framework.

The website provides:

* Home page introducing the RMT practice
* Services page displaying treatment types, durations, and pricing
* About page containing practitioner information
* FAQ page for common questions
* Contact page for submitting inquiries
* Form validation for user-submitted contact information
* Email delivery service for processing contact inquiries

## Technologies

* **C#**
* **.NET 10**
* **Blazor / Razor Components**
* **HTML5**
* **CSS3**
* **Bootstrap**
* **Dependency Injection**
* **HTTP Client**
* **Email Services**

## Key Features

### Service Information

The Services page presents available massage treatments in a structured table containing treatment type, appointment duration, and cost.

### Contact Form

The Contact page provides a validated form allowing visitors to submit:

* Email address
* Subject
* Message

Data annotations are used to validate required fields, email formatting, and maximum input lengths before submission.

### Email Processing

The application uses a dedicated email service registered through dependency injection. Submitted inquiries are passed to the email service and sent using configurable email settings.

### Responsive Navigation

The application uses Blazor navigation components to provide access to the Home, Services, About, Contact, and FAQ sections.

## Project Structure

```text
BlazorApp.RMTWebsite/
├── Components/
│   ├── Layout/
│   │   └── NavMenu.razor
│   └── Pages/
│       ├── Home.razor
│       ├── Services.razor
│       ├── About.razor
│       ├── ContactUs.razor
│       └── ...
├── Models/
├── RMTServices/
├── wwwroot/
├── Program.cs
└── BlazorApp.RMTWebsite.csproj
```

## Technical Implementation

The application registers Razor Components and application services through ASP.NET Core's dependency injection system. An HTTP client and email service are registered during application startup, while email configuration is bound from application settings.

The contact form uses Blazor's `EditForm`, data-annotation validation, model binding, and asynchronous submission handling. After a successful submission, the form is cleared and the user receives a status message indicating whether the inquiry was sent successfully.

## Running the Application

### Prerequisites

* .NET 10 SDK
* Visual Studio 2026 or another compatible .NET development environment

### Clone the Repository

```bash
git clone https://github.com/n-turco/BlazorApp.RMTWebsite.git
cd BlazorApp.RMTWebsite
```

### Run the Application

```bash
dotnet run
```

Alternatively, open the solution in Visual Studio and run the project using the development profile.

## Future Improvements

Potential future development includes:

* Online appointment booking
* Dynamic service management
* Database-backed appointment scheduling
* Additional contact-form fields
* Improved responsive styling
* Administrative functionality
* Automated testing
* Deployment to a production hosting environment

## Purpose

This project demonstrates practical experience developing a modern web application with **C#, .NET, Blazor, Razor Components, HTML/CSS, form validation, dependency injection, and service-based email processing**.
