# Manual Archive & Replace Process for EcommerceStarter

If you prefer to do this manually instead of using the script, follow these steps:

## Prerequisites

- GitHub CLI (`gh`) installed and authenticated
- Or use GitHub web interface

---

## Option 1: Using GitHub CLI (Recommended)

### Step 1: Archive Old Repo
```bash
gh repo archive davidtres03/EcommerceStarter --yes
```

### Step 2: Delete Archived Repo
```bash
gh repo delete davidtres03/EcommerceStarter --yes
```

### Step 3: Create Fresh Repo
```bash
gh repo create EcommerceStarter \
    --public \
    --description "Modern, production-ready e-commerce platform built with ASP.NET Core 8"
```

### Step 4: Push Clean Foundation
```bash
cd /c/Catalyst/Catalyst-Personal/open_source_ready/EcommerceStarter

# Initialize if needed
git init
git add .
git commit -m "Initial commit: Clean EcommerceStarter foundation"

# Push to new repo
git remote add origin https://github.com/davidtres03/EcommerceStarter.git
git branch -M main
git push -u origin main
```

---

## Option 2: Using GitHub Web Interface

### Step 1: Archive Old Repo
1. Go to https://github.com/davidtres03/EcommerceStarter/settings
2. Scroll to "Danger Zone"
3. Click "Archive this repository"
4. Confirm

### Step 2: Delete Archived Repo
1. Stay in Settings → Danger Zone
2. Click "Delete this repository"
3. Type `davidtres03/EcommerceStarter` to confirm
4. Click "I understand, delete this repository"

### Step 3: Create Fresh Repo
1. Go to https://github.com/new
2. Repository name: `EcommerceStarter`
3. Description: "Modern, production-ready e-commerce platform built with ASP.NET Core 8"
4. Public
5. **Do NOT** initialize with README (we'll push ours)
6. Click "Create repository"

### Step 4: Push Clean Foundation
```bash
cd /c/Catalyst/Catalyst-Personal/open_source_ready/EcommerceStarter

# Initialize
git init
git add .
git commit -m "Initial commit: Clean EcommerceStarter foundation"

# Push
git remote add origin https://github.com/davidtres03/EcommerceStarter.git
git branch -M main
git push -u origin main
```

---

## Verification Checklist

After completing the process, verify:

- [ ] New repo exists at https://github.com/davidtres03/EcommerceStarter
- [ ] README.md displays correctly
- [ ] LICENSE file is present
- [ ] .gitignore is working (no bin/obj/publish folders)
- [ ] Only 1 commit in history ("Initial commit")
- [ ] Repo is PUBLIC
- [ ] All 4 projects are present (EcommerceStarter, Installer, Upgrader, WindowsService)

---

## Rollback

If something goes wrong, you can:

1. **Restore archived repo**: Unarchive it from GitHub Settings
2. **Re-run the process**: Delete the new repo and start over

---

## Next Steps After Success

1. **Set up branch protection** (optional)
   - Go to Settings → Branches
   - Add rule for `main` branch
   - Require pull request reviews

2. **Configure GitHub Actions** (optional)
   - Add `.github/workflows/` for CI/CD
   - Auto-build on push
   - Auto-test on PR

3. **Update project links**
   - Update any external references to repo URL
   - Update documentation that points to old repo

4. **Start development**
   - Clone fresh repo to development location
   - Set up local environment
   - Begin feature work!

---

**Need help?** Reach out or run the automated script: `bash scripts/github/archive-and-replace-ecommerce.sh`
