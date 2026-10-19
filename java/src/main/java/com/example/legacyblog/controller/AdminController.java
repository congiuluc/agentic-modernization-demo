package com.example.legacyblog.controller;

import com.example.legacyblog.model.*;
import com.example.legacyblog.service.BlogService;
import com.example.legacyblog.service.BlogProblem;
import com.example.legacyblog.viewmodel.*;
import javax.validation.Valid;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.*;

@Controller
@RequestMapping("/admin")
public class AdminController {
    private final BlogService blogs;
    public AdminController(BlogService blogs) { this.blogs = blogs; }

    @GetMapping
    public String dashboard() { return "admin"; }
    @GetMapping("/posts")
    public String posts(@RequestParam(defaultValue = "1") String page, Model model) {
        int number = blogs.page(page);
        model.addAttribute("posts", blogs.posts(false, null, number));
        model.addAttribute("total", blogs.count(false, null)); model.addAttribute("page", number);
        return "posts";
    }
    @GetMapping("/posts/create")
    public String createPost(Model model) { return postForm(null, new PostForm(), model); }
    @GetMapping("/posts/{id}/edit")
    public String editPost(@PathVariable Long id, Model model) {
        Post value = blogs.post(id); PostForm form = new PostForm();
        form.setTitle(value.getTitle()); form.setSlug(value.getSlug()); form.setBody(value.getBody());
        form.setCategoryId(value.getCategory().getId());
        return postForm(id, form, model);
    }
    @PostMapping({"/posts/create", "/posts/{id}/edit"})
    public String savePost(@PathVariable(required = false) Long id,
                           @Valid @ModelAttribute("form") PostForm form, BindingResult errors, Model model) {
        if (id != null) { blogs.post(id); }
        if (blogs.postSlugTaken(form.getSlug(), id)) { errors.rejectValue("slug", "duplicate", "Slug already exists."); }
        if (form.getCategoryId() != null && blogs.categories().stream().noneMatch(c -> c.getId().equals(form.getCategoryId()))) {
            errors.rejectValue("categoryId", "missing", "Select an existing category.");
        }
        if (errors.hasErrors()) { return postForm(id, form, model); }
        blogs.savePost(id, form); return "redirect:/admin/posts";
    }
    private String postForm(Long id, PostForm form, Model model) {
        model.addAttribute("form", form); model.addAttribute("id", id);
        model.addAttribute("categories", blogs.categories()); return "post-form";
    }
    @GetMapping("/posts/{id}/delete")
    public String deleteConfirmation(@PathVariable Long id, Model model) {
        model.addAttribute("post", blogs.post(id)); return "post-delete";
    }
    @PostMapping("/posts/{id}/delete")
    public String delete(@PathVariable Long id) { blogs.deletePost(id); return "redirect:/admin/posts"; }
    @PostMapping("/posts/{id}/publish")
    public String publish(@PathVariable Long id) { blogs.publish(id, true); return "redirect:/admin/posts"; }
    @PostMapping("/posts/{id}/unpublish")
    public String unpublish(@PathVariable Long id) { blogs.publish(id, false); return "redirect:/admin/posts"; }
    @GetMapping("/categories")
    public String categories(Model model) { model.addAttribute("categories", blogs.categories()); return "categories"; }
    @GetMapping("/categories/create")
    public String createCategory(Model model) { return categoryForm(null, new CategoryForm(), model); }
    @GetMapping("/categories/{id}/edit")
    public String editCategory(@PathVariable Long id, Model model) {
        Category value = blogs.category(id); CategoryForm form = new CategoryForm();
        form.setName(value.getName()); form.setSlug(value.getSlug()); return categoryForm(id, form, model);
    }
    @PostMapping({"/categories/create", "/categories/{id}/edit"})
    public String saveCategory(@PathVariable(required = false) Long id, @Valid @ModelAttribute("form") CategoryForm form,
                               BindingResult errors, Model model) {
        if (id != null) { blogs.category(id); }
        if (blogs.categorySlugTaken(form.getSlug(), id)) { errors.rejectValue("slug", "duplicate", "Slug already exists."); }
        if (errors.hasErrors()) { return categoryForm(id, form, model); }
        blogs.saveCategory(id, form); return "redirect:/admin/categories";
    }
    private String categoryForm(Long id, CategoryForm form, Model model) {
        model.addAttribute("form", form); model.addAttribute("id", id); return "category-form";
    }
    @PostMapping("/categories/{id}/delete")
    public String deleteCategory(@PathVariable Long id, Model model) {
        try { blogs.deleteCategory(id); }
        catch (BlogProblem ex) {
            if (ex.getStatus() != 409) { throw ex; }
            model.addAttribute("error", ex.getMessage()); return categories(model);
        }
        return "redirect:/admin/categories";
    }
    @GetMapping("/comments")
    public String comments(Model model) { model.addAttribute("comments", blogs.pendingComments()); return "comments"; }
    @PostMapping("/comments/{id}/approve")
    public String approveComment(@PathVariable Long id) { blogs.moderate(id, false); return "redirect:/admin/comments"; }
    @PostMapping("/comments/{id}/delete")
    public String deleteComment(@PathVariable Long id) { blogs.moderate(id, true); return "redirect:/admin/comments"; }
}
