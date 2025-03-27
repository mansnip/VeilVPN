"use strict";

// --- عناصر UI ---
const chatInput = document.getElementById("chat-input");
const sendButton = document.querySelector(".chat-send");
const messageContainer = document.getElementById("users-conversation"); // UL برای نمایش پیام‌ها
const userListContainer = document.getElementById("userList"); // UL برای لیست چت‌ها (بیشتر برای ادمین)
// ... سایر عناصر UI که نیاز دارید ...

// --- اتصال SignalR ---
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/chatHub") // همان آدرسی که در Program.cs مپ کردیم
    .configureLogging(signalR.LogLevel.Information) // برای دیباگ
    .build();

// --- متغیرهای وضعیت ---
let currentRecipientUserId = null; // برای ادمین، ID کاربری که در حال چت با اوست
let currentUserName = "شما"; // نام کاربر فعلی (می‌توان از سرور گرفت)

// --- توابع کمکی UI ---

// اضافه کردن پیام به UI
function addMessageToUI(messageData, isSentByCurrentUser) {
    if (!messageData || !messageData.message) return;

    const li = document.createElement("li");
    li.classList.add("chat-list", isSentByCurrentUser ? "right" : "left");

    const conversationDiv = document.createElement("div");
    conversationDiv.classList.add("conversation-list");

    // آواتار (اختیاری) - می‌توانید URL آواتار را هم بفرستید
    if (!isSentByCurrentUser) {
        const avatarDiv = document.createElement("div");
        avatarDiv.classList.add("chat-avatar");
        avatarDiv.innerHTML = `<img src="/assets/images/users/avatar-placeholder.jpg" alt="">`; // یا آواتار واقعی
        conversationDiv.appendChild(avatarDiv);
    }

    const userChatContentDiv = document.createElement("div");
    userChatContentDiv.classList.add("user-chat-content");

    const ctextWrapDiv = document.createElement("div");
    ctextWrapDiv.classList.add("ctext-wrap");

    const ctextWrapContentDiv = document.createElement("div");
    ctextWrapContentDiv.classList.add("ctext-wrap-content");

    const messageP = document.createElement("p");
    messageP.classList.add("mb-0", "ctext-content");
    messageP.textContent = messageData.message; // یا messageData.content
    ctextWrapContentDiv.appendChild(messageP);

    // زمان پیام
    const timeSpan = document.createElement("span");
    timeSpan.classList.add("chat-time", "mb-0", "fs-11");
    // زمان را فرمت کنید (نیاز به کتابخانه مثل moment.js یا توابع Date خود جاوااسکریپت دارید)
    timeSpan.textContent = new Date(messageData.timestamp).toLocaleTimeString('fa-IR', { hour: '2-digit', minute: '2-digit' });
    ctextWrapContentDiv.appendChild(timeSpan);

    ctextWrapDiv.appendChild(ctextWrapContentDiv);
    userChatContentDiv.appendChild(ctextWrapDiv);

    // نام فرستنده (برای پیام‌های دریافتی)
    if (!isSentByCurrentUser) {
        const conversationNameDiv = document.createElement("div");
        conversationNameDiv.classList.add("conversation-name");
        conversationNameDiv.innerHTML = `<span class="display-name">${messageData.senderName || 'کاربر'}</span>`;
        userChatContentDiv.insertBefore(conversationNameDiv, ctextWrapDiv); // قبل از متن پیام
    }


    conversationDiv.appendChild(userChatContentDiv);
    li.appendChild(conversationDiv);
    messageContainer.appendChild(li);

    // اسکرول به پایین
    scrollToBottom();
}

// تابع برای افزودن یک چت به لیست سمت چپ (بیشتر برای ادمین)
function addUserToList(userId, userName, lastMessage, unreadCount) {
    const li = document.createElement("li");
    li.id = `chat-${userId}`; // برای انتخاب راحت‌تر
    li.innerHTML = `
        <a href="javascript: void(0);" class="unread-msg-user">
            <div class="d-flex align-items-center">
                <div class="flex-shrink-0 chat-user-img online align-self-center me-2 ms-0">
                    <div class="avatar-xxs">
                        <img src="/assets/images/users/avatar-placeholder.jpg" class="rounded-circle img-fluid userprofile" alt="">
                    </div>
                    <span class="user-status"></span>
                </div>
                <div class="flex-grow-1 overflow-hidden">
                    <p class="text-truncate mb-0">${userName}</p>
                    <span class="text-truncate fs-11">${lastMessage || ''}</span> 
                </div>
                ${unreadCount > 0 ? `<div class="flex-shrink-0"><span class="badge badge-soft-dark rounded p-1">${unreadCount}</span></div>` : ''}
            </div>
        </a>`;

    // اضافه کردن event listener برای کلیک روی این کاربر
    li.addEventListener('click', () => {
        // TODO: Load chat history for this user
        console.log(`Clicked on user: ${userId}`);
        currentRecipientUserId = userId; // تنظیم کاربر فعلی برای چت (برای ادمین)
        // پاک کردن پیام‌های فعلی
        messageContainer.innerHTML = '';
        // درخواست تاریخچه از سرور
        connection.invoke("LoadChatHistory", userId).catch(err => console.error("LoadChatHistory error:", err.toString()));
        // به‌روزرسانی هدر چت با نام و وضعیت کاربر
        updateChatHeader(userName, true); // فرض کنید آنلاین است
        // TODO: علامت زدن پیام‌ها به عنوان خوانده شده در سرور
        // connection.invoke("MarkMessagesAsRead", userId)...
    });

    userListContainer.appendChild(li);
}

function updateChatHeader(userName, isOnline) {
    const headerUsername = document.querySelector('.user-chat-topbar .username');
    const headerStatus = document.querySelector('.user-chat-topbar .userStatus small');
    if (headerUsername) headerUsername.textContent = userName;
    if (headerStatus) headerStatus.textContent = isOnline ? "آنلاین" : "آفلاین";
    // TODO: آپدیت عکس پروفایل هدر
}


function scrollToBottom() {
    // از SimpleBar استفاده شده، باید از API آن استفاده کرد اگر دارد
    // یا به صورت ساده:
    const chatConversationElement = document.getElementById('chat-conversation'); // Div اصلی که simplebar دارد
    if (chatConversationElement && chatConversationElement.SimpleBar) {
        chatConversationElement.SimpleBar.getScrollElement().scrollTop = chatConversationElement.SimpleBar.getScrollElement().scrollHeight;
    } else if (messageContainer.parentElement) { // Fallback
        messageContainer.parentElement.scrollTop = messageContainer.parentElement.scrollHeight;
    }
}

// --- مدیریت رویدادهای SignalR (سرور به کلاینت) ---

// دریافت پیام جدید
connection.on("ReceiveMessage", (messageData) => {
    console.log("Message received: ", messageData);
    // فرض می‌کنیم messageData شامل senderUserId, message, timestamp, senderName است
    // تشخیص اینکه آیا پیام از کاربر فعلی است یا نه
    // این لاجیک ممکن است نیاز به بهبود داشته باشد، بسته به اینکه سرور چه چیزی می‌فرستد
    const isCurrentUser = messageData.senderUserId === "YOUR_CURRENT_USER_ID"; // باید ID کاربر فعلی را بدانید
    addMessageToUI(messageData, false); // همیشه به عنوان پیام دریافتی اضافه می‌کنیم (چون پیام ارسالی خودمان را جدا مدیریت می‌کنیم)
});

// تایید ارسال پیام توسط خود کاربر (برای آپدیت UI فرستنده)
connection.on("MessageSentConfirmation", (messageData) => {
    console.log("Message sent confirmed: ", messageData);
    addMessageToUI(messageData, true); // پیام ارسال شده توسط خودم را نمایش بده
});


// بارگذاری تاریخچه چت
connection.on("LoadChatHistory", (history) => {
    console.log("History loaded: ", history);
    messageContainer.innerHTML = ''; // پاک کردن پیام‌های قبلی
    if (history && history.length > 0) {
        history.forEach(msg => {
            // تشخیص اینکه پیام ارسالی بوده یا دریافتی
            const isSentByCurrentUser = msg.senderUserId === "YOUR_CURRENT_USER_ID"; // نیاز به ID کاربر فعلی
            addMessageToUI(msg, isSentByCurrentUser);
        });
    }
    scrollToBottom();
});

// (برای ادمین) دریافت لیست چت‌های فعال
connection.on("LoadActiveChats", (chats) => {
    userListContainer.innerHTML = ''; // پاک کردن لیست قبلی
    if (chats && chats.length > 0) {
        chats.forEach(chat => {
            addUserToList(chat.userId, chat.userName, chat.lastMessage, chat.unreadCount);
        });
    }
});

// (برای ادمین) دریافت نوتیفیکیشن پیام جدید از کاربری که چتش باز نیست
connection.on("NotifyNewMessage", (userId, userName) => {
    console.log(`New message from ${userName} (${userId})`);
    // TODO: آپدیت کردن لیست کاربران یا نمایش نوتیفیکیشن
    // ممکن است بخواهید کاربر را در لیست آپدیت کنید یا اگر نیست اضافه کنید
    // و unread count را افزایش دهید
    const userListItem = document.getElementById(`chat-${userId}`);
    if (userListItem) {
        // TODO: Update last message and unread count in the UI list item
        // مثال: آپدیت تعداد پیام‌های خوانده نشده
        const badge = userListItem.querySelector('.badge');
        if (badge) {
            badge.textContent = parseInt(badge.textContent || '0') + 1;
        } else {
            // ایجاد badge اگر وجود نداشت
            const flexShrinkDiv = userListItem.querySelector('.flex-shrink-0:last-child'); // پیدا کردن div سمت چپ
            if (flexShrinkDiv) {
                flexShrinkDiv.innerHTML = `<span class="badge badge-soft-dark rounded p-1">1</span>`;
            }
        }
        // TODO: آپدیت آخرین پیام (lastMessage)
        // const lastMessageSpan = userListItem.querySelector('.text-truncate.fs-11');
        // if(lastMessageSpan) lastMessageSpan.textContent = "پیام جدید...";

        // کاربر را به بالای لیست ببرید (اختیاری)
        userListContainer.prepend(userListItem);

    } else {
        addUserToList(userId, userName, "پیام جدید...", 1); // افزودن به لیست
    }
    // می‌توانید یک toast notification هم نمایش دهید
    // showToast(`پیام جدید از ${userName}`);
});

// خطاهای احتمالی SignalR
connection.on("Error", (errorMessage) => {
    console.error("SignalR Error:", errorMessage);
    // می‌توانید به کاربر پیام خطا نمایش دهید
    // alert("خطا در ارتباط با سرور چت: " + errorMessage);
});

// --- مدیریت رویدادهای UI (کاربر به سرور) ---

// ارسال پیام با کلیک روی دکمه یا فشردن Enter
async function sendMessage() {
    const message = chatInput.value.trim();
    if (!message) {
        // نمایش فیدبک در UI (قالب Velzon این قابلیت را دارد)
        chatInput.classList.add("is-invalid");
        const feedback = document.querySelector(".chat-input-feedback");
        if (feedback) feedback.style.display = 'block';
        return;
    } else {
        chatInput.classList.remove("is-invalid");
        const feedback = document.querySelector(".chat-input-feedback");
        if (feedback) feedback.style.display = 'none';
    }

    // تشخیص گیرنده (برای ادمین مهم است)
    let recipient = null;
    // TODO: بر اساس Role کاربر فعلی (که از سرور باید بگیریم یا در صفحه ست شده باشد)
    // و اینکه آیا ادمین در حال چت با کاربر خاصی است (currentRecipientUserId)
    const IS_ADMIN = false; // این باید داینامیک باشد

    if (IS_ADMIN && currentRecipientUserId) {
        recipient = currentRecipientUserId;
    } // در غیر این صورت، کاربر عادی به ادمین(ها) می‌فرستد یا ادمین به صورت عمومی؟ (نیاز به تعریف سناریو)

    console.log(`Sending message: "${message}" to ${recipient || 'Admins'}`);

    try {
        // صدا زدن متد SendMessage در ChatHub
        // اگر پارامتر recipientUserId را به متد سرور اضافه کرده‌اید، آن را هم بفرستید
        // await connection.invoke("SendMessage", message, recipient); // مثال با گیرنده
        await connection.invoke("SendMessage", message); // مثال ساده

        // پاک کردن اینپوت بعد از ارسال موفق
        chatInput.value = "";

        // نکته: پیام خود کاربر توسط MessageSentConfirmation به UI اضافه می‌شود
        // یا می‌توانیم بلافاصله آن را به UI اضافه کنیم (پیشنهادی برای تجربه کاربری بهتر)
        // addMessageToUI({ message: message, timestamp: new Date().toISOString(), senderName: currentUserName }, true);
        // scrollToBottom();

    } catch (err) {
        console.error("Send message failed:", err.toString());
        alert("خطا در ارسال پیام. لطفا دوباره تلاش کنید.");
    }
}

// Event Listener برای فرم ارسال پیام
const chatInputForm = document.getElementById('chatinput-form');
if (chatInputForm) {
    chatInputForm.addEventListener('submit', (event) => {
        event.preventDefault(); // جلوگیری از رفرش صفحه
        sendMessage();
    });
} else {
    console.error("Chat input form not found!");
}

// Event listener برای دکمه ارسال (اگر جداگانه نیاز باشد، هرچند submit فرم کافی است)
// if (sendButton) {
//     sendButton.addEventListener('click', sendMessage);
// }

// --- شروع اتصال و بارگذاری اولیه ---

async function startConnection() {
    try {
        await connection.start();
        console.log("SignalR Connected.");

        // TODO: بعد از اتصال، اطلاعات اولیه را از سرور بگیرید
        // 1. دریافت UserID کاربر فعلی (اگر از قبل ندارید)
        // const userId = await connection.invoke("GetMyUserId"); // مثال: متدی در هاب که UserId را برگرداند
        // currentUserId = userId; // در متغیر گلوبال ذخیره کنید

        // 2. اگر کاربر ادمین است، لیست چت‌های فعال را درخواست کند
        const IS_ADMIN = false; // این باید داینامیک باشد
        if (IS_ADMIN) {
            connection.invoke("GetActiveChats").catch(err => console.error("GetActiveChats error:", err.toString()));
        } else {
            // اگر کاربر عادی است، تاریخچه چت خودش با ادمین را درخواست کند
            connection.invoke("LoadChatHistory", null).catch(err => console.error("LoadChatHistory error:", err.toString()));
            // (null به معنی چت با ادمین، یا می‌توانید UserId ادمین خاصی را بفرستید اگر سیستم اینطور است)
            // همچنین هدر چت را آپدیت کنید
            updateChatHeader("پشتیبانی رهاگذر", true); // فرض اولیه که پشتیبانی آنلاین است
        }


    } catch (err) {
        console.error("SignalR Connection Error: ", err.toString());
        // تلاش مجدد برای اتصال بعد از چند ثانیه (الگوریتم Backoff)
        setTimeout(startConnection, 5000);
    }
}

// مدیریت قطع شدن اتصال
connection.onclose(async (error) => {
    console.error(`SignalR Connection closed. Error: ${error?.message || 'N/A'}. Trying to reconnect...`);
    // مهم: باید کاربر را از وضعیت مطلع کنید و برای اتصال مجدد تلاش کنید
    await startConnection(); // تلاش مجدد ساده
});

// --- اجرای اولیه ---
// اطمینان از اینکه DOM آماده است (اگر کد در <head> لود می‌شود)
document.addEventListener("DOMContentLoaded", () => {
    // استایل اولیه UI (مثلاً مخفی کردن لودر)
    const loader = document.getElementById('elmLoader');
    if (loader) loader.style.display = 'none';

    // دریافت ID کاربر فعلی (باید از طریق Backend به View پاس داده شود)
    // مثال: currentUserId = document.getElementById('current-user-id-hidden-field').value;
    // یا در یک تگ script در View:
    // <script>
    //    const currentUserId = '@User.FindFirstValue(ClaimTypes.NameIdentifier)'; // مثال در Razor
    // </script>

    // TODO: مقدار دهی متغیر currentUserId در اینجا الزامی است
    // const currentUserId = "USER_ID_FROM_BACKEND"; // <<< این خط باید مقداردهی شود >>>
    // console.log("Current User ID:", currentUserId);


    // شروع اتصال SignalR
    startConnection();
});

// توابع اضافی (که در Velzon ممکن است استفاده شوند)
// - جستجو در پیام‌ها (searchMessages)
// - نمایش/مخفی کردن پروفایل کاربر (userProfileCanvasExample)
// - مدیریت تب‌ها (Chats/Contacts)
// - مدیریت انتخاب emoji (emoji-btn)
// - ...

// // مثال تابع جستجو (باید تکمیل شود)
// function searchMessages() {
//     const searchTerm = document.getElementById('searchMessage').value.toLowerCase();
//     const messages = messageContainer.querySelectorAll('.ctext-content');
//     messages.forEach(msgElement => {
//         const text = msgElement.textContent.toLowerCase();
//         const parentLi = msgElement.closest('li.chat-list');
//         if (parentLi) {
//             if (text.includes(searchTerm)) {
//                 parentLi.style.display = '';
//             } else {
//                 parentLi.style.display = 'none';
//             }
//         }
//     });
// }


// // مثال فعال کردن emoji picker (اگر از کتابخانه خاصی استفاده می‌کند)
// const emojiButton = document.getElementById('emoji-btn');
// if(emojiButton) {
//     // initEmojiPicker(emojiButton, chatInput); // تابعی فرضی برای راه اندازی پیکر
// }

