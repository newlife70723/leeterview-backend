pipeline {
    agent any

    environment {
        COMPOSE_PROJECT_NAME = 'leeterview-backend'
        GIT_REPO = 'git@github.com:newlife70723/leeterview-backend.git'
        BRANCH = 'main'
    }

    stages {
        stage('Clean Docker Environment') {
            steps {
                script {
                    dir('home/ubuntu/leeterview') {
                        sh 'docker-compose down'
                        sh 'docker system prune -a --volumes -f'
                    }
                }
            }
        }

        stage('Pull Lastest Code') {
            steps {
                script {
                    dir('/home/ubuntu/leeterview/leeterview-backend') {
                        withCredentials([sshUserPrivateKey(credentailsId: 'git-ssh-credentials', keyFileVariable: 'SSH_KEY')]) {
                            sh '''
                                eval $(ssh-agent -s)
                                ssh-add ${SSH_KEY}
                                git reset --hard
                                git fetch origin ${BRANCH}
                                git checkout ${BRANCH}
                                git pull origin ${BRANCH}
                            '''
                        }
                    }
                }
            }
        }

        stage('Build and Run with Docker Compose') {
            steps {
                script {
                    dir('/home/ubuntu/leeterview') {
                        sh 'docker-compose up -d --build'
                    }
                }
            }
        }
    }

    post {
        success {
            echo 'Pipeline executed successfully!'
        }
        failure {
            echo 'Pipeline failed!'
        }
    }
}