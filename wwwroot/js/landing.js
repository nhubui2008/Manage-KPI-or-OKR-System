(() => {
    "use strict";

    const root = document.documentElement;
    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    root.classList.toggle("js-motion", !reduceMotion);

    const header = document.querySelector("[data-landing-header]");
    const menuButton = document.querySelector("[data-menu-button]");
    const menu = document.querySelector("[data-menu]");

    const updateHeader = () => header?.classList.toggle("is-scrolled", window.scrollY > 8);
    updateHeader();
    window.addEventListener("scroll", updateHeader, { passive: true });

    const setMenu = (open) => {
        if (!menuButton || !menu) return;
        menuButton.setAttribute("aria-expanded", String(open));
        menu.classList.toggle("is-open", open);
        const label = menuButton.querySelector(".visually-hidden");
        if (label) label.textContent = open ? "Đóng menu" : "Mở menu";
    };

    menuButton?.addEventListener("click", () => {
        setMenu(menuButton.getAttribute("aria-expanded") !== "true");
    });
    menu?.querySelectorAll("a").forEach((link) => link.addEventListener("click", () => setMenu(false)));
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && menuButton?.getAttribute("aria-expanded") === "true") {
            setMenu(false);
            menuButton.focus();
        }
    });
    const desktopMenuQuery = window.matchMedia("(min-width: 901px)");
    const handleDesktopMenu = (event) => {
        if (event.matches) setMenu(false);
    };
    if (typeof desktopMenuQuery.addEventListener === "function") {
        desktopMenuQuery.addEventListener("change", handleDesktopMenu);
    } else if (typeof desktopMenuQuery.addListener === "function") {
        desktopMenuQuery.addListener(handleDesktopMenu);
    }

    const revealItems = Array.from(document.querySelectorAll("[data-reveal]"));
    if (reduceMotion || !("IntersectionObserver" in window)) {
        revealItems.forEach((item) => item.classList.add("is-visible"));
    } else {
        const revealObserver = new IntersectionObserver((entries, observer) => {
            entries.forEach((entry) => {
                if (!entry.isIntersecting) return;
                entry.target.classList.add("is-visible");
                observer.unobserve(entry.target);
            });
        }, { rootMargin: "0px 0px -8% 0px", threshold: 0.08 });
        revealItems.forEach((item) => revealObserver.observe(item));
    }

    document.querySelectorAll("[data-role-tabs]").forEach((component) => {
        const tabs = Array.from(component.querySelectorAll('[role="tab"]'));
        const panels = Array.from(component.querySelectorAll('[role="tabpanel"]'));

        const activate = (tab, focus = false) => {
            tabs.forEach((item) => {
                const selected = item === tab;
                item.setAttribute("aria-selected", String(selected));
                item.tabIndex = selected ? 0 : -1;
            });
            panels.forEach((panel) => {
                panel.hidden = panel.id !== tab.getAttribute("aria-controls");
            });
            if (focus) tab.focus();
        };

        tabs.forEach((tab, index) => {
            tab.addEventListener("click", () => activate(tab));
            tab.addEventListener("keydown", (event) => {
                let nextIndex = index;
                if (event.key === "ArrowRight") nextIndex = (index + 1) % tabs.length;
                else if (event.key === "ArrowLeft") nextIndex = (index - 1 + tabs.length) % tabs.length;
                else if (event.key === "Home") nextIndex = 0;
                else if (event.key === "End") nextIndex = tabs.length - 1;
                else return;
                event.preventDefault();
                activate(tabs[nextIndex], true);
            });
        });
    });

    document.querySelectorAll("[data-dashboard-carousel]").forEach((component) => {
        const slides = Array.from(component.querySelectorAll("[data-dashboard-slide]"));
        const dots = Array.from(component.querySelectorAll("[data-carousel-dot]"));
        const previousButton = component.querySelector("[data-carousel-prev]");
        const nextButton = component.querySelector("[data-carousel-next]");
        const caption = component.closest("figure")?.querySelector("[data-carousel-caption]");
        const viewport = component.querySelector(".landing-dashboard-viewport");
        let activeIndex = 0;
        let wheelLocked = false;
        let touchStartX = null;
        let touchStartY = null;
        let transitionTimer = null;

        const activate = (nextIndex, announce = true) => {
            const boundedIndex = Math.max(0, Math.min(nextIndex, slides.length - 1));
            const previousIndex = activeIndex;
            const changed = boundedIndex !== previousIndex;
            const direction = boundedIndex > previousIndex ? "next" : "previous";

            if (transitionTimer) window.clearTimeout(transitionTimer);
            slides.forEach((slide) => {
                slide.classList.remove(
                    "is-leaving",
                    "is-entering-next",
                    "is-leaving-next",
                    "is-entering-previous",
                    "is-leaving-previous"
                );
            });

            if (changed && !reduceMotion) {
                slides[previousIndex]?.classList.add("is-leaving", `is-leaving-${direction}`);
                slides[boundedIndex]?.classList.add(`is-entering-${direction}`);
                transitionTimer = window.setTimeout(() => {
                    slides.forEach((slide) => slide.classList.remove(
                        "is-leaving",
                        "is-entering-next",
                        "is-leaving-next",
                        "is-entering-previous",
                        "is-leaving-previous"
                    ));
                    transitionTimer = null;
                }, 480);
            }

            activeIndex = boundedIndex;

            slides.forEach((slide, index) => {
                const active = index === activeIndex;
                slide.classList.toggle("is-active", active);
                slide.classList.toggle("is-before", index < activeIndex);
                slide.setAttribute("aria-hidden", String(!active));
                if ("inert" in slide) slide.inert = !active;
            });

            dots.forEach((dot, index) => {
                const selected = index === activeIndex;
                dot.setAttribute("aria-selected", String(selected));
                dot.tabIndex = selected ? 0 : -1;
            });

            if (previousButton) previousButton.disabled = activeIndex === 0;
            if (nextButton) nextButton.disabled = activeIndex === slides.length - 1;
            if (caption) {
                caption.textContent = slides[activeIndex]?.dataset.slideTitle || "Màn hình hệ thống";
                caption.setAttribute("aria-live", announce ? "polite" : "off");
            }
        };

        previousButton?.addEventListener("click", () => activate(activeIndex - 1));
        nextButton?.addEventListener("click", () => activate(activeIndex + 1));
        dots.forEach((dot, index) => dot.addEventListener("click", () => activate(index)));

        component.addEventListener("keydown", (event) => {
            if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
            const target = event.target instanceof Element ? event.target : null;
            if (target?.closest("canvas")) return;

            const button = target?.closest("button");
            const carouselDot = button?.matches("[data-carousel-dot]") === true;
            if (button && !carouselDot) return;

            event.preventDefault();
            let nextIndex = activeIndex;
            if (event.key === "Home") nextIndex = 0;
            else if (event.key === "End") nextIndex = slides.length - 1;
            else nextIndex += event.key === "ArrowRight" ? 1 : -1;

            nextIndex = Math.max(0, Math.min(nextIndex, slides.length - 1));
            activate(nextIndex);
            if (carouselDot) dots[nextIndex]?.focus();
        });

        component.addEventListener("wheel", (event) => {
            if (wheelLocked || Math.abs(event.deltaY) < 24 || Math.abs(event.deltaY) < Math.abs(event.deltaX)) return;
            const direction = event.deltaY > 0 ? 1 : -1;
            const nextIndex = activeIndex + direction;
            if (nextIndex < 0 || nextIndex >= slides.length) return;
            event.preventDefault();
            activate(nextIndex);
            wheelLocked = true;
            window.setTimeout(() => { wheelLocked = false; }, reduceMotion ? 80 : 420);
        }, { passive: false });

        viewport?.addEventListener("touchstart", (event) => {
            const touch = event.changedTouches[0];
            touchStartX = touch.clientX;
            touchStartY = touch.clientY;
        }, { passive: true });
        viewport?.addEventListener("touchend", (event) => {
            if (touchStartX === null || touchStartY === null) return;
            const touch = event.changedTouches[0];
            const deltaX = touch.clientX - touchStartX;
            const deltaY = touch.clientY - touchStartY;
            touchStartX = null;
            touchStartY = null;
            if (Math.abs(deltaX) < 45 || Math.abs(deltaX) <= Math.abs(deltaY)) return;
            activate(activeIndex + (deltaX < 0 ? 1 : -1));
        }, { passive: true });

        activate(0, false);
    });

    const createNativeProgressChart = (canvas, labels, values, label) => {
        const wrapper = canvas.parentElement;
        const tooltip = document.createElement("div");
        tooltip.className = "landing-chart-tooltip";
        tooltip.hidden = true;
        tooltip.setAttribute("role", "status");
        tooltip.setAttribute("aria-live", "polite");
        wrapper.appendChild(tooltip);

        const context = canvas.getContext("2d");
        let points = [];
        let activeIndex = -1;

        const draw = () => {
            const width = Math.max(canvas.clientWidth, 240);
            const height = Math.max(canvas.clientHeight, 120);
            const pixelRatio = Math.min(window.devicePixelRatio || 1, 2);
            canvas.width = Math.round(width * pixelRatio);
            canvas.height = Math.round(height * pixelRatio);
            context.setTransform(pixelRatio, 0, 0, pixelRatio, 0, 0);
            context.clearRect(0, 0, width, height);

            const padding = { top: 14, right: 12, bottom: 27, left: 32 };
            const chartWidth = width - padding.left - padding.right;
            const chartHeight = height - padding.top - padding.bottom;

            context.font = '8px "Times New Roman", Times, serif';
            context.textAlign = "right";
            context.textBaseline = "middle";
            for (let step = 0; step <= 4; step += 1) {
                const value = step * 25;
                const y = padding.top + chartHeight - (value / 100) * chartHeight;
                context.beginPath();
                context.strokeStyle = "#e8eef5";
                context.lineWidth = 1;
                context.moveTo(padding.left, y);
                context.lineTo(width - padding.right, y);
                context.stroke();
                context.fillStyle = "#718096";
                context.fillText(`${value}%`, padding.left - 5, y);
            }

            points = values.map((value, index) => ({
                x: padding.left + (index / Math.max(values.length - 1, 1)) * chartWidth,
                y: padding.top + chartHeight - (value / 100) * chartHeight
            }));

            context.beginPath();
            points.forEach((point, index) => index ? context.lineTo(point.x, point.y) : context.moveTo(point.x, point.y));
            context.strokeStyle = "#1677ff";
            context.lineWidth = 2.5;
            context.lineJoin = "round";
            context.lineCap = "round";
            context.stroke();

            context.textAlign = "center";
            context.textBaseline = "top";
            labels.forEach((item, index) => {
                const point = points[index];
                context.fillStyle = "#718096";
                context.fillText(item.replace("Tháng ", "T"), point.x, height - padding.bottom + 9);
                context.beginPath();
                context.arc(point.x, point.y, index === activeIndex ? 5 : 3, 0, Math.PI * 2);
                context.fillStyle = index === activeIndex ? "#1677ff" : "#ffffff";
                context.fill();
                context.strokeStyle = "#1677ff";
                context.lineWidth = 2;
                context.stroke();
            });

            if (activeIndex >= 0) showTooltip(activeIndex, false);
        };

        const showTooltip = (index, redraw = true) => {
            activeIndex = Math.max(0, Math.min(index, values.length - 1));
            const point = points[activeIndex];
            if (!point) return;
            tooltip.textContent = `${labels[activeIndex]} · ${label}: ${values[activeIndex]}%`;
            tooltip.style.left = `${point.x}px`;
            tooltip.style.top = `${point.y}px`;
            tooltip.hidden = false;
            canvas.setAttribute("aria-label", `${label}, ${labels[activeIndex]}: ${values[activeIndex]} phần trăm. Dùng phím mũi tên để xem các mốc.`);
            if (redraw) draw();
        };

        const selectFromPointer = (event) => {
            if (!points.length) return;
            const bounds = canvas.getBoundingClientRect();
            const x = event.clientX - bounds.left;
            const nearest = points.reduce((best, point, index) =>
                Math.abs(point.x - x) < Math.abs(points[best].x - x) ? index : best, 0);
            showTooltip(nearest);
        };

        canvas.addEventListener("pointermove", selectFromPointer);
        canvas.addEventListener("pointerdown", selectFromPointer);
        canvas.addEventListener("pointerleave", () => {
            if (document.activeElement !== canvas) {
                activeIndex = -1;
                tooltip.hidden = true;
                draw();
            }
        });
        canvas.addEventListener("focus", () => showTooltip(activeIndex < 0 ? values.length - 1 : activeIndex));
        canvas.addEventListener("blur", () => {
            activeIndex = -1;
            tooltip.hidden = true;
            draw();
        });
        canvas.addEventListener("keydown", (event) => {
            if (!["ArrowLeft", "ArrowRight", "Home", "End"].includes(event.key)) return;
            event.preventDefault();
            if (event.key === "Home") showTooltip(0);
            else if (event.key === "End") showTooltip(values.length - 1);
            else showTooltip(activeIndex + (event.key === "ArrowRight" ? 1 : -1));
        });

        new ResizeObserver(draw).observe(wrapper);
        draw();
    };

    const createProgressChart = (canvas, labels, values, label) => {
        if (!canvas) return;
        if (typeof window.Chart === "undefined") {
            createNativeProgressChart(canvas, labels, values, label);
            return;
        }

        new window.Chart(canvas, {
            type: "line",
            data: {
                labels,
                datasets: [{
                    label,
                    data: values,
                    borderColor: "#1677ff",
                    backgroundColor: "#1677ff",
                    pointBackgroundColor: "#ffffff",
                    pointBorderColor: "#1677ff",
                    pointBorderWidth: 2,
                    pointRadius: 3.5,
                    pointHoverRadius: 6,
                    pointHitRadius: 18,
                    borderWidth: 2,
                    tension: 0.34,
                    fill: false
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: reduceMotion ? false : { duration: 480 },
                interaction: { intersect: false, mode: "index" },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        displayColors: false,
                        callbacks: { label: (context) => `${context.dataset.label}: ${context.parsed.y}%` }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        ticks: { color: "#718096", font: { family: "Times New Roman", size: 8 }, maxRotation: 0 }
                    },
                    y: {
                        beginAtZero: true,
                        suggestedMax: 100,
                        grid: { color: "#e8eef5" },
                        border: { display: false },
                        ticks: {
                            color: "#718096",
                            font: { family: "Times New Roman", size: 8 },
                            callback: (value) => `${value}%`
                        }
                    }
                }
            }
        });
    };

    createProgressChart(
        document.getElementById("landingProgressChart"),
        ["Tuần 1", "Tuần 2", "Tuần 3", "Tuần 4", "Tuần 5", "Tuần 6"],
        [51, 57, 61, 66, 72, 78],
        "Tiến độ"
    );

    createProgressChart(
        document.getElementById("landingWorkspaceChart"),
        ["Tháng 3", "Tháng 4", "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8"],
        [61, 65, 64, 71, 75, 80],
        "KPI hoàn thành"
    );
})();
