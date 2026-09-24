---
name: new-page
description: Scaffold a new routable Blazor page in this site following project conventions (PageTitle, meta description, single h1, scoped CSS, nav link). Use when asked to add a new page such as Policies, Book, or New Patients.
---

# New page

Arguments: page name and route, e.g. `Policies /policies`. Ask if either is missing.

1. Create `BlazorApp.RMTWebsite/Components/Pages/<Name>.razor`:
   ```razor
   @page "/<route>"

   <PageTitle><Readable Title> | Kinetic Flow RMT</PageTitle>
   <HeadContent>
       <meta name="description" content="<150-160 character summary>" />
   </HeadContent>

   <h1><Readable Title></h1>

   <section>
       ...
   </section>
   ```
2. Create `<Name>.razor.css` next to it only if the page needs styles beyond Bootstrap. No inline styles.
3. Add a link in `Components/Layout/NavMenu.razor` and the footer Quick Links in
   `Components/Layout/MainLayout.razor` if the page should be navigable.
4. If the page shows editable content (prices, FAQ, hours), put the data in a model/config rather
   than hard-coding it in markup.
5. Run `dotnet build` and confirm zero warnings, then run the site and check the page at 375px and
   desktop widths.
