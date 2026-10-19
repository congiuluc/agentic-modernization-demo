package com.example.legacyblog.controller;

import com.example.legacyblog.model.*;
import com.example.legacyblog.service.BlogService;
import com.example.legacyblog.viewmodel.CommentForm;
import javax.validation.Valid;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.validation.BindingResult;
import org.springframework.web.bind.annotation.*;

@Controller
public class PublicController {
    private final BlogService blogs;
    public PublicController(BlogService blogs) { this.blogs = blogs; }

    @GetMapping({"/", "/categories/{slug}"})
    public String index(@PathVariable(required = false) String slug,
                        @RequestParam(defaultValue = "1") String page, Model model) {
        int number = blogs.page(page);
        Category category = slug == null ? null : blogs.categoryBySlug(slug);
        Long categoryId = category == null ? null : category.getId();
        model.addAttribute("posts", blogs.posts(true, categoryId, number));
        model.addAttribute("total", blogs.count(true, categoryId));
        model.addAttribute("page", number); model.addAttribute("category", category);
        model.addAttribute("categories", blogs.categories());
        return "index";
    }
    @GetMapping("/posts/{slug}")
    public String detail(@PathVariable String slug, Model model) {
        return detailModel(slug, new CommentForm(), model);
    }
    @PostMapping("/posts/{slug}/comments")
    public String comment(@PathVariable String slug, @Valid @ModelAttribute("form") CommentForm form,
                          BindingResult errors, Model model) {
        blogs.publicPost(slug);
        if (errors.hasErrors()) { return detailModel(slug, form, model); }
        blogs.comment(slug, form);
        return "redirect:/posts/" + slug + "?submitted=1";
    }
    private String detailModel(String slug, CommentForm form, Model model) {
        Post value = blogs.publicPost(slug);
        model.addAttribute("post", value);
        model.addAttribute("comments", blogs.approvedComments(value.getId()));
        model.addAttribute("form", form);
        return "detail";
    }
    @GetMapping("/account/login")
    public String login() { return "login"; }
}
