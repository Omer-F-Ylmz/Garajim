(function () {
    "use strict";

    var TOKEN_KEY = "garajim_token";
    var USER_KEY = "garajim_user";

    var state = {
        token: null,
        user: null,
        vehicles: [],
        selectedVehicleId: null,
        chart: null
    };

    var MAINTENANCE_TYPES = [
        ["PeriyodikBakim", "Periyodik bakım"],
        ["YagDegisimi", "Yağ değişimi"],
        ["FrenBakimi", "Fren bakımı"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["AkuDegisimi", "Akü değişimi"],
        ["Diger", "Diğer"]
    ];

    var FUEL_TYPES = [
        ["Benzin", "Benzin"],
        ["Dizel", "Dizel"],
        ["Lpg", "LPG"],
        ["Hibrit", "Hibrit"],
        ["Elektrik", "Elektrik"]
    ];

    var EXPENSE_CATEGORIES = [
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["Mtv", "MTV"],
        ["Muayene", "Muayene"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Otopark", "Otopark"],
        ["Kopru", "Köprü / otoyol"],
        ["TrafikCezasi", "Trafik cezası"],
        ["Yikama", "Yıkama"],
        ["Diger", "Diğer"]
    ];

    var REMINDER_TYPES = [
        ["Muayene", "Muayene"],
        ["TrafikSigortasi", "Trafik sigortası"],
        ["Kasko", "Kasko"],
        ["EgzozEmisyon", "Egzoz emisyon"],
        ["Mtv", "MTV"],
        ["PeriyodikBakim", "Periyodik bakım"],
        ["LastikDegisimi", "Lastik değişimi"],
        ["Diger", "Diğer"]
    ];

    var PRICE_FUEL = ["Benzin", "Dizel", "LPG & Benzin", "Hibrit", "Elektrik"];
    var PRICE_GEAR = ["Düz", "Otomatik", "Yarı Otomatik"];
    var PRICE_BODY = ["Sedan", "Hatchback/5", "Hatchback/3", "Station wagon", "MPV", "Coupe", "SUV", "Cabrio", "Roadster"];

    var moneyFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    var wholeFormat = new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 0 });
    var literFormat = new Intl.NumberFormat("tr-TR", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function el(id) {
        return document.getElementById(id);
    }

    function clear(node) {
        while (node.firstChild) {
            node.removeChild(node.firstChild);
        }
    }

    function make(tag, text, className) {
        var node = document.createElement(tag);
        if (text !== undefined && text !== null) {
            node.textContent = String(text);
        }
        if (className) {
            node.className = className;
        }
        return node;
    }

    function labelOf(pairs, value) {
        for (var i = 0; i < pairs.length; i++) {
            if (pairs[i][0] === value) {
                return pairs[i][1];
            }
        }
        return value === null || value === undefined ? "" : String(value);
    }

    function fillSelect(select, pairs) {
        clear(select);
        pairs.forEach(function (pair) {
            var option = document.createElement("option");
            option.value = pair[0];
            option.textContent = pair[1];
            select.appendChild(option);
        });
    }

    function fillSimpleSelect(select, values) {
        fillSelect(select, values.map(function (value) { return [value, value]; }));
    }

    function money(value) {
        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return moneyFormat.format(number) + " TL";
    }

    function km(value) {
        var number = Number(value);
        if (!isFinite(number)) {
            return "-";
        }
        return wholeFormat.format(number) + " km";
    }

    function formatDate(value) {
        if (!value) {
            return "-";
        }
        var date = new Date(value);
        if (isNaN(date.getTime())) {
            return "-";
        }
        return date.toLocaleDateString("tr-TR");
    }

    function todayInput() {
        var now = new Date();
        var month = String(now.getMonth() + 1).padStart(2, "0");
        var day = String(now.getDate()).padStart(2, "0");
        return now.getFullYear() + "-" + month + "-" + day;
    }

    function showMessage(node, text, isOk) {
        node.textContent = text || "";
        node.className = isOk ? "message ok" : "message";
    }

    function clearMessages() {
        showMessage(el("auth-message"), "");
        showMessage(el("app-message"), "");
    }

    function saveSession(token, user) {
        state.token = token;
        state.user = user;
        localStorage.setItem(TOKEN_KEY, token);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
    }

    function clearSession() {
        state.token = null;
        state.user = null;
        state.vehicles = [];
        state.selectedVehicleId = null;
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
    }

    function readSession() {
        var token = localStorage.getItem(TOKEN_KEY);
        if (!token) {
            return false;
        }
        state.token = token;
        try {
            state.user = JSON.parse(localStorage.getItem(USER_KEY) || "null");
        } catch (error) {
            state.user = null;
        }
        return true;
    }

    function goToLogin(message) {
        clearSession();
        if (state.chart) {
            state.chart.destroy();
            state.chart = null;
        }
        el("app-screen").classList.add("hidden");
        el("auth-screen").classList.remove("hidden");
        showMessage(el("auth-message"), message || "");
    }

    function api(path, options) {
        var settings = options || {};
        var headers = { "Accept": "application/json" };
        if (state.token) {
            headers.Authorization = "Bearer " + state.token;
        }
        var init = { method: settings.method || "GET", headers: headers };
        if (settings.body !== undefined) {
            headers["Content-Type"] = "application/json";
            init.body = JSON.stringify(settings.body);
        }

        var isAuthCall = path.indexOf("/api/Auth/") === 0;

        return fetch(path, init).then(function (response) {
            if (response.status === 401 && !isAuthCall) {
                goToLogin("Oturum süreniz doldu, lütfen tekrar giriş yapın.");
                throw new Error("Oturum gerekli.");
            }
            return response.text().then(function (text) {
                var payload = null;
                if (text) {
                    try {
                        payload = JSON.parse(text);
                    } catch (error) {
                        payload = null;
                    }
                }
                if (payload && payload.success === false) {
                    throw new Error(payload.message || "İşlem başarısız.");
                }
                if (!response.ok) {
                    throw new Error(readProblem(payload) || "Sunucu hatası (" + response.status + ").");
                }
                return payload;
            });
        });
    }

    function readProblem(payload) {
        if (!payload) {
            return null;
        }
        if (payload.errors) {
            var keys = Object.keys(payload.errors);
            if (keys.length > 0) {
                var first = payload.errors[keys[0]];
                if (Array.isArray(first) && first.length > 0) {
                    return "Geçersiz alan: " + keys[0];
                }
            }
        }
        return payload.title || payload.message || null;
    }

    function handleError(node, error) {
        var text = error && error.message ? error.message : "Beklenmeyen bir hata oluştu.";
        if (text === "Oturum gerekli.") {
            return;
        }
        if (text === "Failed to fetch") {
            text = "Sunucuya ulaşılamadı.";
        }
        showMessage(node, text);
    }

    function switchAuthTab(showLogin) {
        el("tab-login").classList.toggle("active", showLogin);
        el("tab-register").classList.toggle("active", !showLogin);
        el("login-form").classList.toggle("hidden", !showLogin);
        el("register-form").classList.toggle("hidden", showLogin);
        showMessage(el("auth-message"), "");
    }

    function enterApp() {
        el("auth-screen").classList.add("hidden");
        el("app-screen").classList.remove("hidden");
        var label = state.user && state.user.fullName ? state.user.fullName : "";
        el("user-label").textContent = label;
        loadVehicles();
    }

    function loadVehicles() {
        return api("/api/Vehicles").then(function (result) {
            state.vehicles = (result && result.data) || [];
            var select = el("vehicle-select");
            clear(select);

            state.vehicles.forEach(function (vehicle) {
                var option = document.createElement("option");
                option.value = String(vehicle.id);
                option.textContent = vehicle.plate + " - " + vehicle.brand + " " + vehicle.model;
                select.appendChild(option);
            });

            var hasVehicles = state.vehicles.length > 0;
            el("empty-state").classList.toggle("hidden", hasVehicles);
            el("workspace").classList.toggle("hidden", !hasVehicles);
            select.classList.toggle("hidden", !hasVehicles);

            if (!hasVehicles) {
                state.selectedVehicleId = null;
                return;
            }

            var stillThere = state.vehicles.some(function (vehicle) {
                return vehicle.id === state.selectedVehicleId;
            });
            if (!stillThere) {
                state.selectedVehicleId = state.vehicles[0].id;
            }
            select.value = String(state.selectedVehicleId);
            loadActiveTab();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function activeTab() {
        var button = document.querySelector(".tab-btn.active");
        return button ? button.getAttribute("data-tab") : "bakim";
    }

    function loadActiveTab() {
        var tab = activeTab();
        if (tab === "bakim") {
            loadMaintenance();
        } else if (tab === "yakit") {
            loadFuel();
        } else if (tab === "masraf") {
            loadExpenses();
        } else if (tab === "hatirlatma") {
            loadReminders();
        } else if (tab === "rapor") {
            loadFuelStats();
            loadMonthly();
        }
    }

    function emptyRow(tbody, columns, text) {
        var row = document.createElement("tr");
        row.className = "empty-row";
        var cell = make("td", text);
        cell.colSpan = columns;
        row.appendChild(cell);
        tbody.appendChild(row);
    }

    function deleteButton(onClick) {
        var cell = document.createElement("td");
        var button = make("button", "Sil", "link-btn");
        button.type = "button";
        button.addEventListener("click", onClick);
        cell.appendChild(button);
        return cell;
    }

    function loadMaintenance() {
        var tbody = el("maintenance-rows");
        api("/api/Maintenance?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 6, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", labelOf(MAINTENANCE_TYPES, item.type)));
                tr.appendChild(make("td", km(item.km)));
                tr.appendChild(make("td", money(item.cost)));
                tr.appendChild(make("td", item.serviceName || "-"));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Maintenance/" + item.id, loadMaintenance); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadFuel() {
        var tbody = el("fuel-rows");
        api("/api/Fuel?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", km(item.km)));
                tr.appendChild(make("td", literFormat.format(Number(item.liters)) + " L"));
                tr.appendChild(make("td", money(item.totalCost)));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Fuel/" + item.id, loadFuel); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadExpenses() {
        var tbody = el("expense-rows");
        api("/api/Expenses?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Kayıt yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", formatDate(item.date)));
                tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
                tr.appendChild(make("td", money(item.amount)));
                tr.appendChild(make("td", item.note || "-"));
                tr.appendChild(deleteButton(function () { removeRecord("/api/Expenses/" + item.id, loadExpenses); }));
                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function loadReminders() {
        var tbody = el("reminder-rows");
        api("/api/Reminders?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            clear(tbody);
            if (rows.length === 0) {
                emptyRow(tbody, 5, "Hatırlatma yok.");
                return;
            }
            rows.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", labelOf(REMINDER_TYPES, item.type)));
                tr.appendChild(make("td", item.dueDate ? formatDate(item.dueDate) : "-"));
                tr.appendChild(make("td", item.dueKm ? km(item.dueKm) : "-"));

                var statusCell = document.createElement("td");
                statusCell.appendChild(make("span", item.isCompleted ? "Tamamlandı" : "Bekliyor", item.isCompleted ? "badge done" : "badge"));
                tr.appendChild(statusCell);

                var actionCell = document.createElement("td");
                if (!item.isCompleted) {
                    var completeButton = make("button", "Tamamla", "link-btn");
                    completeButton.type = "button";
                    completeButton.addEventListener("click", function () { completeReminder(item.id); });
                    actionCell.appendChild(completeButton);
                }
                var deleteLink = make("button", "Sil", "link-btn");
                deleteLink.type = "button";
                deleteLink.addEventListener("click", function () { removeRecord("/api/Reminders/" + item.id, loadReminders); });
                actionCell.appendChild(deleteLink);
                tr.appendChild(actionCell);

                tbody.appendChild(tr);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });

        loadUpcoming();
    }

    function loadUpcoming() {
        var list = el("upcoming-list");
        api("/api/Reminders/upcoming?days=30").then(function (result) {
            var rows = (result && result.data) || [];
            clear(list);
            if (rows.length === 0) {
                list.appendChild(make("li", "Önümüzdeki 30 günde hatırlatma yok."));
                return;
            }
            rows.forEach(function (item) {
                var li = document.createElement("li");
                var left = document.createElement("div");
                left.appendChild(make("div", labelOf(REMINDER_TYPES, item.type) + " - " + item.plate));
                left.appendChild(make("div", formatDate(item.dueDate), "muted"));
                li.appendChild(left);

                var button = make("button", "Tamamla", "ghost");
                button.type = "button";
                button.addEventListener("click", function () { completeReminder(item.id); });
                li.appendChild(button);

                list.appendChild(li);
            });
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function completeReminder(id) {
        api("/api/Reminders/" + id + "/complete", { method: "PUT" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Hatırlatma tamamlandı.", true);
            loadReminders();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function removeRecord(path, reload) {
        api(path, { method: "DELETE" }).then(function (result) {
            showMessage(el("app-message"), (result && result.message) || "Kayıt silindi.", true);
            reload();
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function card(label, value, accent) {
        var box = make("div", null, accent ? "card accent" : "card");
        box.appendChild(make("span", label, "card-label"));
        box.appendChild(make("span", value, "card-value"));
        return box;
    }

    function loadSummary() {
        var start = el("report-start").value;
        var end = el("report-end").value;
        var cards = el("summary-cards");
        var tbody = el("category-rows");

        return api("/api/Reports/summary?vehicleId=" + state.selectedVehicleId + "&start=" + start + "&end=" + end).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);
            cards.appendChild(card("Yakıt", money(data.totalFuel)));
            cards.appendChild(card("Bakım", money(data.totalMaintenance)));
            cards.appendChild(card("Diğer masraf", money(data.totalOtherExpense)));
            cards.appendChild(card("Genel toplam", money(data.grandTotal), true));

            clear(tbody);
            var categories = data.categories || [];
            if (categories.length === 0) {
                emptyRow(tbody, 2, "Bu aralıkta masraf yok.");
                return;
            }
            categories.forEach(function (item) {
                var tr = document.createElement("tr");
                tr.appendChild(make("td", labelOf(EXPENSE_CATEGORIES, item.category)));
                tr.appendChild(make("td", money(item.total)));
                tbody.appendChild(tr);
            });
        });
    }

    function loadFuelStats() {
        var cards = el("fuel-cards");
        return api("/api/Reports/fuel-stats?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var data = (result && result.data) || {};
            clear(cards);
            cards.appendChild(card("Ortalama tüketim", literFormat.format(Number(data.averageConsumptionPer100Km)) + " L/100km", true));
            cards.appendChild(card("Km başına maliyet", money(data.costPerKm), true));
            cards.appendChild(card("Toplam mesafe", km(data.totalKm)));
            cards.appendChild(card("Toplam yakıt", literFormat.format(Number(data.totalLiters)) + " L"));
            cards.appendChild(card("Toplam tutar", money(data.totalCost)));
        }).catch(function (error) {
            clear(cards);
            cards.appendChild(card("Yakıt istatistiği", error && error.message ? error.message : "Hesaplanamadı."));
        });
    }

    function loadMonthly() {
        return api("/api/Reports/monthly?vehicleId=" + state.selectedVehicleId).then(function (result) {
            var rows = (result && result.data) || [];
            drawChart(rows);
        }).catch(function (error) {
            handleError(el("app-message"), error);
        });
    }

    function drawChart(rows) {
        var fallback = el("chart-fallback");
        var canvas = el("monthly-chart");

        if (typeof Chart === "undefined") {
            fallback.textContent = "Grafik kütüphanesi yüklenemedi (CDN erişimi yok). Aylık toplamlar tabloda görünmeye devam eder.";
            return;
        }

        fallback.textContent = rows.length === 0 ? "Grafik için henüz veri yok." : "";

        var labels = rows.map(function (item) {
            return String(item.month).padStart(2, "0") + "." + item.year;
        });
        var values = rows.map(function (item) { return Number(item.total); });

        if (state.chart) {
            state.chart.destroy();
        }

        state.chart = new Chart(canvas, {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: "Aylık toplam (TL)",
                    data: values,
                    backgroundColor: "rgba(255, 122, 26, 0.65)",
                    borderColor: "#ff7a1a",
                    borderWidth: 1,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { labels: { color: "#99a1ad" } }
                },
                scales: {
                    x: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } },
                    y: { ticks: { color: "#99a1ad" }, grid: { color: "#262b33" } }
                }
            }
        });
    }

    function bindAuth() {
        el("tab-login").addEventListener("click", function () { switchAuthTab(true); });
        el("tab-register").addEventListener("click", function () { switchAuthTab(false); });

        el("login-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Auth/login", {
                method: "POST",
                body: {
                    email: el("login-email").value,
                    password: el("login-password").value
                }
            }).then(function (result) {
                saveSession(result.data.token, result.data);
                el("login-password").value = "";
                enterApp();
            }).catch(function (error) {
                handleError(el("auth-message"), error);
            });
        });

        el("register-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Auth/register", {
                method: "POST",
                body: {
                    fullName: el("register-name").value,
                    email: el("register-email").value,
                    password: el("register-password").value
                }
            }).then(function (result) {
                saveSession(result.data.token, result.data);
                el("register-password").value = "";
                enterApp();
            }).catch(function (error) {
                handleError(el("auth-message"), error);
            });
        });

        el("logout-btn").addEventListener("click", function () {
            goToLogin("Çıkış yapıldı.");
        });
    }

    function bindVehicle() {
        el("vehicle-select").addEventListener("change", function (event) {
            state.selectedVehicleId = Number(event.target.value);
            clearMessages();
            loadActiveTab();
        });

        el("add-vehicle-btn").addEventListener("click", function () {
            el("vehicle-form-box").classList.remove("hidden");
            el("vehicle-year").value = new Date().getFullYear();
        });

        el("vehicle-cancel").addEventListener("click", function () {
            el("vehicle-form-box").classList.add("hidden");
        });

        el("vehicle-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Vehicles", {
                method: "POST",
                body: {
                    plate: el("vehicle-plate").value,
                    brand: el("vehicle-brand").value,
                    model: el("vehicle-model").value,
                    year: Number(el("vehicle-year").value),
                    currentKm: Number(el("vehicle-km").value),
                    fuelType: el("vehicle-fuel").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Araç eklendi.", true);
                el("vehicle-form").reset();
                el("vehicle-form-box").classList.add("hidden");
                state.selectedVehicleId = result.data.id;
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });
    }

    function bindTabs() {
        var buttons = document.querySelectorAll(".tab-btn");
        Array.prototype.forEach.call(buttons, function (button) {
            button.addEventListener("click", function () {
                Array.prototype.forEach.call(buttons, function (other) {
                    other.classList.remove("active");
                });
                button.classList.add("active");

                var tab = button.getAttribute("data-tab");
                var panels = document.querySelectorAll(".tab-panel");
                Array.prototype.forEach.call(panels, function (panel) {
                    panel.classList.add("hidden");
                });
                el("panel-" + tab).classList.remove("hidden");
                clearMessages();
                loadActiveTab();
            });
        });
    }

    function bindRecordForms() {
        el("maintenance-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Maintenance", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    type: el("maintenance-type").value,
                    date: el("maintenance-date").value,
                    km: Number(el("maintenance-km").value),
                    cost: Number(el("maintenance-cost").value),
                    serviceName: el("maintenance-service").value,
                    note: el("maintenance-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("maintenance-form").reset();
                el("maintenance-date").value = todayInput();
                loadMaintenance();
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("fuel-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Fuel", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    date: el("fuel-date").value,
                    km: Number(el("fuel-km").value),
                    liters: Number(el("fuel-liters").value),
                    totalCost: Number(el("fuel-cost").value)
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("fuel-form").reset();
                el("fuel-date").value = todayInput();
                loadFuel();
                loadVehicles();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("expense-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/Expenses", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    category: el("expense-category").value,
                    date: el("expense-date").value,
                    amount: Number(el("expense-amount").value),
                    note: el("expense-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Kayıt eklendi.", true);
                el("expense-form").reset();
                el("expense-date").value = todayInput();
                loadExpenses();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("reminder-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            var dateValue = el("reminder-date").value;
            var kmValue = el("reminder-km").value;
            api("/api/Reminders", {
                method: "POST",
                body: {
                    vehicleId: state.selectedVehicleId,
                    type: el("reminder-type").value,
                    dueDate: dateValue ? dateValue : null,
                    dueKm: kmValue ? Number(kmValue) : null,
                    note: el("reminder-note").value
                }
            }).then(function (result) {
                showMessage(el("app-message"), (result && result.message) || "Hatırlatma eklendi.", true);
                el("reminder-form").reset();
                loadReminders();
            }).catch(function (error) {
                handleError(el("app-message"), error);
            });
        });

        el("report-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            loadSummary().catch(function (error) {
                handleError(el("app-message"), error);
            });
            loadMonthly();
            loadFuelStats();
        });

        el("price-form").addEventListener("submit", function (event) {
            event.preventDefault();
            clearMessages();
            api("/api/price/estimate", {
                method: "POST",
                body: {
                    marka: el("price-marka").value,
                    seri: el("price-seri").value,
                    yil: Number(el("price-yil").value),
                    kilometre: Number(el("price-km").value),
                    yakitTipi: el("price-yakit").value,
                    vitesTipi: el("price-vites").value,
                    kasaTipi: el("price-kasa").value
                }
            }).then(function (result) {
                var data = result.data;
                el("price-result").classList.remove("hidden");
                el("price-value").textContent = wholeFormat.format(Number(data.tahminiFiyat)) + " " + data.paraBirimi;
                el("price-detail").textContent = data.yil + " " + data.marka + " " + data.seri + " - " + km(data.kilometre);
            }).catch(function (error) {
                el("price-result").classList.add("hidden");
                handleError(el("app-message"), error);
            });
        });
    }

    function initSelects() {
        fillSelect(el("vehicle-fuel"), FUEL_TYPES);
        fillSelect(el("maintenance-type"), MAINTENANCE_TYPES);
        fillSelect(el("expense-category"), EXPENSE_CATEGORIES);
        fillSelect(el("reminder-type"), REMINDER_TYPES);
        fillSimpleSelect(el("price-yakit"), PRICE_FUEL);
        fillSimpleSelect(el("price-vites"), PRICE_GEAR);
        fillSimpleSelect(el("price-kasa"), PRICE_BODY);
    }

    function initDates() {
        var today = todayInput();
        el("maintenance-date").value = today;
        el("fuel-date").value = today;
        el("expense-date").value = today;
        el("report-end").value = today;
        var start = new Date();
        start.setMonth(start.getMonth() - 6);
        el("report-start").value = start.toISOString().slice(0, 10);
    }

    function init() {
        initSelects();
        initDates();
        bindAuth();
        bindVehicle();
        bindTabs();
        bindRecordForms();

        if (readSession()) {
            enterApp();
        }
    }

    document.addEventListener("DOMContentLoaded", init);
})();
