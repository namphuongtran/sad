/*
 * This is a Design for Real-Time Vocabulary Quiz system
 *
*/
workspace "Vocabulary Quiz System" "This is a Real-Time Vocabulary Quiz system." {

    model {
        user = person "User" "A learner who uses this system to answer questions in real-time." "User"        

        group "Vocabulary Quiz System" {

            vocabularyQuizSystem = softwaresystem "Vocabulary Quiz System" "Allows users to interact with the quiz, submit answers, and view the leaderboard." {

                singlePageApplication = container "Single-Page Application" "Provides quiz functionality to users via their web browser." "JavaScript and ReactJS" "Web Browser"
                mobileApp = container "Mobile App" "Allows users to interact with the quiz, submit answers, and view the leaderboard via their mobile device." "React Native" "Mobile App"
                
                identityApplication = container "Identity Server" "Handles user authentication and authorization." "Duende Identity Server" "Identity Provider" {
                    homeController = component "Home Controller" "Handles the default route to the SPA route ensuring users are signed in before doing any actions." "ASP.NET MVC Rest Controller"
                    signinController = component "Sign In Controller" "Allows users to sign in to the Vocabulary Quiz System." "ASP.NET MVC Rest Controller"
                    accountController = component "Accounts Controller" "Provides functionalities such as Reset Password, Forgot Password, etc." "ASP.NET MVC Rest Controller"
                    signoutController = component "Sign Out Controller" "Allows users to sign out of the Vocabulary Quiz System." "ASP.NET MVC Rest Controller"
                    registerController = component "Register Controller" "Provides functionalities related to user registration." "ASP.NET MVC Rest Controller"
                    errorController = component "Error Controller" "Handles errors when users interact with the Vocabulary Quiz System." "ASP.NET MVC Rest Controller" 
                    securityComponent = component "Security Component" "Provides functionality related to signing in, changing passwords, etc." "C#"                    
                }
                
                apiApplication = container "API Application" "Handles user's answers with Quiz system, displays scoring, and manages data persistence via a JSON/HTTPS API." "Python and Django" {
                    quizController = component "Quiz management for the quiz session" "Python"
                    answersController = component "Answers Controller" "Handles answer submission and validation." "Python"
                    scoringController = component "Scoring Controller" "Calculates and retrieves scores from the database." "Python"
                    leaderboardController = component "Leaderboard Controller" "Manages and retrieves real-time leaderboard standings." "Python"
                    quizComponent = component "Quiz Logic Processing" "Handles the core quiz logic." "Python"
                }
                
                realTimeServer  = container "Real-Time Server" "Manages real-time communication between clients and the server." "Manages real-time for Web App" "Python with Django Channels"
                pushNotification = container "Push Notification Service" "Handles push notifications to mobile apps." "Firebase Cloud Messaging (FCM) for Android, Apple Push Notification Service (APNS) for iOS"
                
                backgroundWorker = container "Background Worker" "Processes quiz logic, score updates, and leaderboard updates." "Python with Celery" {                    
                    scoreUpdater = component "Score Updater" "Manages real-time score updates." "Python with Celery"
                    leaderboardUpdater = component "Leaderboard Updater" "Maintains and updates the real-time leaderboard." "Python with Celery"
                    notificationSender = component "Notification Sender" "Sends push notifications to users." "Python with Celery"
                    taskScheduler = component "Task Scheduler" "Schedules and manages periodic tasks." "Python with Celery Beat"
                }                
                
                database = container "Database" "Stores quiz data, user information, scores, and leaderboard standings." "MongoDB" "Database"
                redis = container "Cache" "Handles real-time leaderboard updates and session management." "Redis" "Cache"
                messageQueue = container "Message Queue" "Handles communication between API Application, Real-Time Server, Notification Hub, and Background Worker." "RabbitMQ"
            }
        }

        # relationships between people and software systems
        user -> vocabularyQuizSystem "Views Leaderboard, scoring, and answer the quiz"        

        # relationships to/from containers
        user -> identityApplication "Login to the system" "HTTPS"
        user -> singlePageApplication "Views Leaderboard, scoring, and submits answers for the quiz"
        user -> mobileApp "Views Leaderboard, scoring, and submits answers for the quiz"

        singlePageApplication -> apiApplication "Make an API call to fetch the latest data (updated scores or leaderboard)."
        apiApplication -> singlePageApplication "Delivers the data to the user's web browser"
        apiApplication -> messageQueue "Processes the request, stores the answer, and places a message in the messageQueue for further processing"
        
        backgroundWorker -> messageQueue "Picks up the message from the queue, processes the answer (e.g., validates, scores it), updates the database"
        backgroundWorker -> realTimeServer "Pushes a message to indicate that new data is available"
        backgroundWorker -> pushNotification "Push the content notification"
        pushNotification -> mobileApp "Broadcast message to the device"
        
        realTimeServer -> singlePageApplication "Notify all connected clients about the updated data"

        apiApplication -> redis "Retrieve or Save the data to the Redis"
        identityApplication -> redis "Retrieve or Save the token or short user to the Redis"

        identityApplication -> database "Retrieve or Save user info to the database"
        apiApplication -> database "Retrieve or Save the data"
        backgroundWorker -> database "Retrieve or Save the data"
        backgroundWorker -> redis "Retrieve or Save the data"

        # relationships to/from components
        singlePageApplication -> signinController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> quizController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> answersController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> scoringController "Makes API calls to" "JSON/HTTPS"
        singlePageApplication -> leaderboardController "Makes API calls to" "JSON/HTTPS"
        
        mobileApp -> signinController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> answersController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> scoringController "Makes API calls to" "JSON/HTTPS"
        mobileApp -> leaderboardController "Makes API calls to" "JSON/HTTPS"
        
        quizController -> quizComponent "Uses"
        answersController -> quizComponent "Uses"
        scoringController -> quizComponent "Uses"
        leaderboardController -> quizComponent "Uses"

        quizComponent -> realTimeServer "Connect to"
        quizComponent -> messageQueue "Send messages"
        quizComponent -> redis "Retrieve or Set"
        quizComponent -> database "Reads from and writes to" "SQL/TCP"

        scoreUpdater -> messageQueue "Read messages"
        scoreUpdater -> realTimeServer "Publish message to Websocket"
        scoreUpdater -> database "Fetches updated score data from the database"

        leaderboardUpdater -> messageQueue "Subscribes to leaderboard update events"
        leaderboardUpdater -> database "Fetches updated leaderboard data from the database"
        leaderboardUpdater -> redis "Updates the leaderboard data in Redis"
        leaderboardUpdater -> realTimeServer "Publishes leaderboard update event"

        signinController -> securityComponent "Uses"
        accountController -> securityComponent "Uses"
        registerController -> securityComponent "Uses"
        signoutController -> securityComponent "Uses"
        securityComponent -> database "Reads from and writes to" "SQL/TCP"
        securityComponent -> redis "Retrieve or Set"
    }

    views {
        systemlandscape "SystemLandscape" {
            include *
            autoLayout
        }

        systemcontext vocabularyQuizSystem "SystemContext" {
            include *
            animation {
                vocabularyQuizSystem
                user
            }
            autoLayout
            description "The system context diagram for the Vocabulary Quiz System."
            properties {
                structurizr.groups false
            }
        }

        container vocabularyQuizSystem "Containers" {
            include *
            animation {
                user identityApplication singlePageApplication mobileApp
                apiApplication
                backgroundWorker
                redis
                database
                pushNotification
                realTimeServer
            }
            autoLayout
            description "The container diagram for the Vocabulary Quiz System."
        }

        component apiApplication "APIComponents" {
            include *
            animation {
                singlePageApplication mobileApp database redis backgroundWorker
                quizController quizComponent
                answersController
                scoringController
                leaderboardController
            }
            autoLayout
            description "The component diagram for the API Application."
        }

        component identityApplication "IdentityComponents" {
            include *
            animation {
                singlePageApplication mobileApp database redis
                signinController securityComponent                
            }
            autoLayout
            description "The component diagram for the Identity Application."
        }

        dynamic identityApplication "SignIn" "Summarizes how the sign-in feature works in the single-page application." {
            singlePageApplication -> signinController "Submits credentials to"
            signinController -> securityComponent "Validates credentials using"
            securityComponent -> redis "Checks user exists in Redis?"
            securityComponent -> database "Select * from users where username = ?"            
            database -> securityComponent "Returns user data to"
            securityComponent -> signinController "Returns true if the hashed password matches"
            signinController -> singlePageApplication "Sends back an authentication token to"
            autoLayout
            description "Summarizes how the sign-in feature works in the single-page application."
        }

        dynamic apiApplication "DataFlow" "Summarizes how data flows through the system from when a user joins a quiz to when the leaderboard is updated" {
            singlePageApplication -> quizController "Submits quiz ID to"
            quizController -> quizComponent "Validates quiz ID"
            quizComponent -> realTimeServer "Connect to and open a quiz session using WebSockets"
            singlePageApplication -> answersController "User submits an answer"
            answersController -> quizComponent "Validates the answer and publishes a score update event"
            quizComponent -> messageQueue "Publishes the score update event"
            scoreUpdater -> messageQueue "Subscribes to score update events"
            scoreUpdater -> database "Fetches updated score data from the database"
            scoreUpdater -> messageQueue "Publishes leaderboard update event"
            leaderboardUpdater -> messageQueue "Subscribes to leaderboard update events"
            leaderboardUpdater -> database "Fetches updated leaderboard data from the database"
            leaderboardUpdater -> redis "Updates the leaderboard data in Redis"
            leaderboardUpdater -> realTimeServer "Publishes leaderboard update event"
            realTimeServer -> singlePageApplication "Pushes the updated leaderboard to all connected clients"
            autoLayout
            description "Summarizes how data flows through the system from when a user joins a quiz to when the leaderboard is updated"
        }
       

        styles {
            element "Person" {
                color #ffffff
                fontSize 22
                shape Person
            }
            element "User" {
                background #08427b
            }            
            element "Software System" {
                background #1168bd
                color #ffffff
            }
            element "Existing System" {
                background #999999
                color #ffffff
            }
            element "Container" {
                background #438dd5
                color #ffffff
            }
            element "Web Browser" {
                shape WebBrowser
            }
            element "Mobile App" {
                shape MobileDeviceLandscape
            }
            element "Database" {
                shape Cylinder
            }
            element "Redis" {
                shape Cylinder
            }
            element "Component" {
                background #85bbf0
                color #000000
            }
            element "Failover" {
                opacity 25
            }
        }
    }
}
