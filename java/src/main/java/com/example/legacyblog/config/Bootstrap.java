package com.example.legacyblog.config;

import com.example.legacyblog.service.BlogService;

public class Bootstrap {
    private final BlogService blogs;
    private final String username;
    private final String password;
    public Bootstrap(BlogService blogs, String username, String password) {
        this.blogs = blogs; this.username = username; this.password = password;
    }
    public void initialize() { blogs.bootstrap(username, password); }
}
