# Contributing to EcommerceStarter

First off, thank you for considering contributing to EcommerceStarter! It's people like you that make this project such a great tool for the community.

## ?? Ways to Contribute

### ?? Report Bugs
Found a bug? Help us squash it!

- Check if the bug has already been reported in [Issues](https://github.com/yourusername/EcommerceStarter/issues)
- If not, [create a new issue](https://github.com/yourusername/EcommerceStarter/issues/new)
- Use the bug report template
- Include as much detail as possible:
  - Steps to reproduce
  - Expected behavior
  - Actual behavior
  - Screenshots (if applicable)
  - Environment details (.NET version, OS, etc.)

### ?? Suggest Features
Have an idea for a new feature?

- Check [existing feature requests](https://github.com/yourusername/EcommerceStarter/labels/enhancement)
- If it's new, [create a feature request](https://github.com/yourusername/EcommerceStarter/issues/new)
- Explain the problem your feature solves
- Describe your proposed solution
- Consider alternatives

### ?? Improve Documentation
Documentation is crucial! Help make it better:

- Fix typos or unclear explanations
- Add examples or clarifications
- Create tutorials or guides
- Improve code comments
- Update outdated information

### ?? Submit Code
Ready to write some code?

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## ?? Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server or SQL Server Express
- Visual Studio 2022 / VS Code / Rider
- Git

### Setup Development Environment

1. **Fork and Clone**
   ```bash
   git clone https://github.com/YOUR-USERNAME/EcommerceStarter.git
   cd EcommerceStarter
   ```

2. **Create a Branch**
   ```bash
   git checkout -b feature/your-feature-name
   # OR
   git checkout -b fix/bug-description
   ```

3. **Set Up Database**
   ```bash
   cd EcommerceStarter
   dotnet ef database update
   ```

4. **Configure Secrets** (optional for testing)
   ```bash
   dotnet user-secrets init
   dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
   dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Test Your Changes**
   - Access: https://localhost:7001
   - Admin: admin@example.com / Admin@123

## ?? Development Guidelines

### Code Style

We follow standard C# and ASP.NET Core conventions:

- **Naming Conventions**
  - PascalCase for classes, methods, properties
  - camelCase for local variables, parameters
  - _camelCase for private fields
  - Meaningful, descriptive names

- **Formatting**
  - 4 spaces for indentation (no tabs)
  - Opening braces on new line
  - One statement per line
  - Use var when type is obvious

- **Comments**
  - XML documentation for public APIs
  - Inline comments for complex logic
  - TODO comments for future improvements
  - Explain "why", not "what"

### Example:
```csharp
/// <summary>
/// Calculates the total price including tax
/// </summary>
/// <param name="subtotal">Order subtotal before tax</param>
/// <param name="taxRate">Tax rate as decimal (e.g., 0.08 for 8%)</param>
/// <returns>Total including tax</returns>
public decimal CalculateTotal(decimal subtotal, decimal taxRate)
{
    // Apply tax only if rate is greater than zero
    if (taxRate > 0)
    {
        return subtotal * (1 + taxRate);
    }
    
    return subtotal;
}
```

### Architecture Principles

- **Separation of Concerns** - Keep business logic in services
- **Dependency Injection** - Use constructor injection
- **SOLID Principles** - Follow best practices
- **DRY** - Don't Repeat Yourself
- **KISS** - Keep It Simple, Stupid
- **Single Responsibility** - One class, one job

### Project Structure

```
EcommerceStarter/
??? Data/               # Database context and migrations
??? Models/             # Domain models
??? Pages/              # Razor Pages
??? Services/           # Business logic services
??? ViewComponents/     # Reusable view components
??? wwwroot/            # Static files
```

## ?? Testing

### Before Submitting PR

- [ ] Code compiles without errors or warnings
- [ ] All existing features still work
- [ ] New features work as expected
- [ ] Tested on different browsers (if UI change)
- [ ] Tested with different screen sizes (if UI change)
- [ ] No breaking changes (unless discussed)
- [ ] Updated documentation (if needed)

### Testing Checklist

- [ ] Can create new products
- [ ] Can add products to cart
- [ ] Can complete checkout process
- [ ] Admin panel is accessible
- [ ] Email notifications work (if configured)
- [ ] Dark mode works
- [ ] Mobile responsive

## ?? Pull Request Process

### 1. Before You Submit

- Update your branch with latest from main:
  ```bash
  git checkout main
  git pull origin main
  git checkout your-branch
  git rebase main
  ```

- Make sure your commits are clean and logical
- Update documentation if needed
- Add your name to contributors (if not already there)

### 2. Submit Pull Request

- Use a clear, descriptive title
- Fill out the PR template
- Link related issues
- Describe what changed and why
- Include screenshots for UI changes
- Request review from maintainers

### 3. PR Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issues
Fixes #123

## Testing
How you tested this

## Screenshots (if applicable)
Before/After screenshots

## Checklist
- [ ] Code compiles
- [ ] Tests pass
- [ ] Documentation updated
- [ ] No breaking changes
```

### 4. Review Process

- Maintainers will review your PR
- Address feedback and comments
- Make requested changes
- Once approved, PR will be merged

## ?? UI/UX Guidelines

### Design Principles
- **Mobile First** - Design for mobile, enhance for desktop
- **Accessibility** - WCAG AA compliance minimum
- **Consistency** - Use existing patterns
- **Bootstrap 5** - Leverage Bootstrap components
- **Dark Mode** - Support both light and dark themes

### UI Checklist
- [ ] Responsive on mobile (320px+)
- [ ] Responsive on tablet (768px+)
- [ ] Responsive on desktop (1024px+)
- [ ] Works in light mode
- [ ] Works in dark mode
- [ ] Keyboard accessible
- [ ] Screen reader friendly
- [ ] Loading states implemented
- [ ] Error states handled

## ?? Security

### Security Guidelines

- **Never commit secrets** - Use User Secrets or environment variables
- **Validate all input** - Never trust user input
- **Parameterize queries** - Use EF Core or parameterized SQL
- **Sanitize output** - Prevent XSS attacks
- **Use HTTPS** - Always in production
- **Follow OWASP** - Web security best practices

### Reporting Security Issues

**Do NOT open public issues for security vulnerabilities!**

Instead:
1. Email security@yourproject.com (if configured)
2. OR create a private security advisory on GitHub
3. Include detailed description
4. Wait for maintainer response

## ?? Documentation

### Where to Add Documentation

- **Code Comments** - For complex logic
- **XML Documentation** - For public APIs
- **README.md** - For project overview
- **docs/** - For detailed guides
- **CHANGELOG.md** - For version history

### Documentation Style

- Clear and concise
- Examples when helpful
- Screenshots for UI features
- Step-by-step instructions
- Links to related docs

## ?? Recognition

Contributors are recognized in:
- GitHub Contributors list
- Project README
- Release notes
- Social media announcements (with permission)

## ?? Communication

### Where to Ask Questions

- **GitHub Discussions** - For general questions
- **GitHub Issues** - For bugs and features
- **Pull Requests** - For code review questions
- **Discord** - For real-time chat (if available)

### Communication Guidelines

- Be respectful and professional
- Assume good intentions
- Provide context
- Be patient
- Help others when you can

## ?? Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## ? Questions?

If you have questions about contributing:

1. Check existing documentation
2. Search closed issues
3. Ask in GitHub Discussions
4. Open a new issue with the "question" label

## ?? Thank You!

Every contribution, no matter how small, makes a difference. Thank you for helping make EcommerceStarter better!

---

**Happy Coding!** ???

---

*This document is inspired by open-source contribution guidelines from various successful projects.*
