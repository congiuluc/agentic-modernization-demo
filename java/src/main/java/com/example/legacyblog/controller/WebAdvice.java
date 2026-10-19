package com.example.legacyblog.controller;

import com.example.legacyblog.service.BlogProblem;
import com.example.legacyblog.viewmodel.*;
import javax.servlet.http.HttpServletRequest;
import javax.servlet.http.HttpServletResponse;
import org.slf4j.*;
import org.springframework.dao.DataIntegrityViolationException;
import org.springframework.ui.Model;
import org.springframework.web.bind.WebDataBinder;
import org.springframework.web.bind.annotation.*;
import org.springframework.web.method.annotation.MethodArgumentTypeMismatchException;
import org.springframework.web.servlet.ModelAndView;
import org.springframework.web.servlet.mvc.support.DefaultHandlerExceptionResolver;

@ControllerAdvice
public class WebAdvice {
    private static final Logger LOG = LoggerFactory.getLogger(WebAdvice.class);
    private final DefaultHandlerExceptionResolver frameworkErrors = new DefaultHandlerExceptionResolver();

    @InitBinder
    public void bind(WebDataBinder binder) {
        if (binder.getTarget() instanceof PostForm) { binder.setAllowedFields("title", "slug", "body", "categoryId"); }
        if (binder.getTarget() instanceof CategoryForm) { binder.setAllowedFields("name", "slug"); }
        if (binder.getTarget() instanceof CommentForm) { binder.setAllowedFields("author", "body"); }
    }
    @ExceptionHandler(BlogProblem.class)
    public String problem(BlogProblem ex, HttpServletResponse response, Model model) {
        response.setStatus(ex.getStatus()); model.addAttribute("error", ex.getMessage()); return "error";
    }
    @ExceptionHandler(MethodArgumentTypeMismatchException.class)
    public String invalid(MethodArgumentTypeMismatchException ex, HttpServletResponse response, Model model) {
        response.setStatus(400); model.addAttribute("error", "Invalid request value."); return "error";
    }
    @ExceptionHandler(DataIntegrityViolationException.class)
    public String conflict(DataIntegrityViolationException ex, HttpServletResponse response, Model model) {
        LOG.warn("Database constraint rejected a conflicting change", ex);
        response.setStatus(409); model.addAttribute("error", "This change conflicts with existing data. Reload the form and try again."); return "error";
    }
    @ExceptionHandler(Exception.class)
    public ModelAndView unexpected(Exception ex, HttpServletRequest request, HttpServletResponse response) {
        ModelAndView frameworkError = frameworkErrors.resolveException(request, response, null, ex);
        if (frameworkError != null) { return frameworkError; }
        LOG.error("Unhandled blog request failure", ex);
        response.setStatus(500);
        return new ModelAndView("error", "error", "An unexpected error occurred. Check the server log.");
    }
}
